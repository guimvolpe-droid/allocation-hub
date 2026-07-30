using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Serialization;
using AllocationHub.Core.Abstractions;
using AllocationHub.Core.Ai;
using AllocationHub.Core.Matching;
using AllocationHub.Core.Sourcing;
using AllocationHub.Infrastructure.Ai;
using AllocationHub.Infrastructure.Data;
using AllocationHub.Infrastructure.Matching;
using AllocationHub.Infrastructure.Security;
using AllocationHub.Infrastructure.Sourcing;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
// Local dev binds localhost:5080; in a container ASPNETCORE_URLS (e.g. http://+:8080) takes over.
if (string.IsNullOrWhiteSpace(builder.Configuration["ASPNETCORE_URLS"]))
    builder.WebHost.UseUrls("http://localhost:5080");

// ---- JSON: serialize enums as strings (nicer contract for the Angular client) ----
builder.Services.AddControllers().AddJsonOptions(o =>
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// ---- Persistence (EF Core) ----
// The database is a CONFIGURATION detail, not a code one: Core has no idea which engine is behind EF,
// so switching SQLite <-> PostgreSQL (Supabase) is one line here and zero lines in the domain.
// Set SUPABASE_DB_CONNECTION (env) — or Database:Provider=postgres + ConnectionStrings:Default — for Postgres.
var supabaseConn = builder.Configuration["SUPABASE_DB_CONNECTION"];
var usePostgres = !string.IsNullOrWhiteSpace(supabaseConn)
    || string.Equals(builder.Configuration["Database:Provider"], "postgres", StringComparison.OrdinalIgnoreCase);
var dbConnection = !string.IsNullOrWhiteSpace(supabaseConn)
    ? supabaseConn
    : builder.Configuration.GetConnectionString("Default");

builder.Services.AddDbContext<AppDbContext>(o =>
{
    // Retry on transient failures: a pooled connection the Supabase pooler dropped while the app sat
    // idle would otherwise surface as one ugly 500 on the next request. No transactions are used, so
    // the retrying execution strategy is safe.
    if (usePostgres) o.UseNpgsql(dbConnection, npg => npg.EnableRetryOnFailure());
    else o.UseSqlite(dbConnection);
});

// ---- Domain / application services ----
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
builder.Services.AddSingleton(jwt);
builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();
// The AI hook: deterministic by default. Swap this single line for an LLM impl (behind the same
// interface) to get model-written explanations — without touching the matching rule.
builder.Services.AddScoped<IMatchExplanationService, DeterministicMatchExplanationService>();
builder.Services.AddScoped<MatchingService>();
builder.Services.AddScoped<AllocationHub.Infrastructure.Data.AuditWriter>();

// ---- External candidate sourcing: real profiles from the GitHub API ----
// GitHub requires a User-Agent. A GITHUB_TOKEN is optional but lifts the rate limit 60 -> 5000 req/h.
builder.Services.AddHttpClient<GitHubCandidateSource>(c =>
{
    c.BaseAddress = new Uri("https://api.github.com/");
    c.Timeout = TimeSpan.FromSeconds(15);
    c.DefaultRequestHeaders.UserAgent.ParseAdd("AllocationHub/1.0");
    c.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
    c.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
    var token = builder.Configuration["GITHUB_TOKEN"] ?? builder.Configuration["GitHub:Token"];
    if (!string.IsNullOrWhiteSpace(token))
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
});
builder.Services.AddScoped<ICandidateSource>(sp => sp.GetRequiredService<GitHubCandidateSource>());

// ---- Multi-LLM: several OpenAI-compatible providers, resolved by name, switchable per request ----
// A provider is only usable when enabled AND its API key is present; otherwise the app falls back to the
// deterministic explanation. Keys come from the environment (.env), never from appsettings/git.
var llmOptions = builder.Configuration.GetSection("Llm").Get<LlmOptions>() ?? new LlmOptions();
builder.Services.AddSingleton(llmOptions);
builder.Services.AddHttpClient("llm", c => c.Timeout = TimeSpan.FromSeconds(llmOptions.TimeoutSeconds));
builder.Services.AddSingleton<ILlmProviderRegistry>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var config = sp.GetRequiredService<IConfiguration>();
    return new LlmProviderRegistry(
        llmOptions,
        () => factory.CreateClient("llm"),
        envName => config[envName] ?? Environment.GetEnvironmentVariable(envName));
});
builder.Services.AddScoped<ILlmExplanationService, LlmExplanationService>();

// ---- Google sign-in (optional; the SPA only shows the button when a client id is configured) ----
var googleAuth = new GoogleAuthOptions
{
    ClientId = builder.Configuration["GOOGLE_CLIENT_ID"] ?? builder.Configuration["Auth:Google:ClientId"],
    AllowedEmails = builder.Configuration["GOOGLE_ALLOWED_EMAILS"] ?? builder.Configuration["Auth:Google:AllowedEmails"],
};
builder.Services.AddSingleton(googleAuth);
builder.Services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();

// ---- Auth ----
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = jwt.Issuer,
        ValidateAudience = true, ValidAudience = jwt.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
        ValidateLifetime = true
    });
builder.Services.AddAuthorization();

// ---- CORS for the Angular dev server ----
var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));

// ---- Swagger with JWT bearer support ----
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AllocationHub API", Version = "v1" });
    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "bearer",
        BearerFormat = "JWT", In = ParameterLocation.Header,
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    c.AddSecurityDefinition("Bearer", scheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = Array.Empty<string>() });
});

var app = builder.Build();

// ---- Create + seed the database on startup ----
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    var log = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    log.LogInformation("Database provider: {Provider}", usePostgres ? "PostgreSQL (Supabase)" : "SQLite (local file)");
    await DbSeeder.SeedAsync(db, hasher);
    log.LogInformation("Database ready — {Consultants} consultants, {Demands} demands seeded/present.",
        db.Consultants.Count(), db.Demands.Count());
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
