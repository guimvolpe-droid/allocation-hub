using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using AllocationHub.Core.Domain;
using AllocationHub.Core.Sourcing;

namespace AllocationHub.Infrastructure.Sourcing;

/// <summary>
/// Real candidate source backed by the official GitHub REST API. Given a demand, it searches public
/// developers by the primary required skill (mapped to a GitHub language), reads each profile and its
/// repositories, and infers a normalized skill set + a deterministic seniority. It talks to the network
/// but hides that entirely behind <see cref="ICandidateSource"/> — the matching rule never sees GitHub.
///
/// Why GitHub and not LinkedIn: LinkedIn has no compliant people-search API and scraping breaks its ToS.
/// GitHub's API is official, free and purpose-built for reading public developer profiles.
/// </summary>
public class GitHubCandidateSource : ICandidateSource
{
    private readonly HttpClient _http;

    public GitHubCandidateSource(HttpClient http) => _http = http;

    public string Name => "github";

    // GitHub 'language' / repo-topic  ->  our canonical skill vocabulary (what demands are written in).
    private static readonly Dictionary<string, string> ToCanonicalSkill = new(StringComparer.OrdinalIgnoreCase)
    {
        ["c#"] = ".NET", ["f#"] = ".NET", ["csharp"] = ".NET", ["dotnet"] = ".NET", [".net"] = ".NET",
        ["aspnetcore"] = ".NET", ["asp.net"] = ".NET", ["aspnet"] = ".NET", ["efcore"] = ".NET",
        ["typescript"] = "TypeScript", ["javascript"] = "JavaScript",
        ["angular"] = "Angular", ["react"] = "React", ["reactjs"] = "React", ["vue"] = "Vue",
        ["kafka"] = "Kafka", ["apache-kafka"] = "Kafka",
        ["sql-server"] = "SQL Server", ["sqlserver"] = "SQL Server", ["mssql"] = "SQL Server", ["tsql"] = "SQL Server",
        ["postgresql"] = "PostgreSQL", ["postgres"] = "PostgreSQL",
        ["mongodb"] = "MongoDB", ["redis"] = "Redis",
        ["python"] = "Python", ["java"] = "Java", ["go"] = "Go", ["golang"] = "Go", ["rust"] = "Rust",
        ["azure"] = "Azure", ["aws"] = "AWS", ["gcp"] = "GCP",
        ["docker"] = "Docker", ["kubernetes"] = "Kubernetes", ["k8s"] = "Kubernetes",
        ["rag"] = "RAG", ["langchain"] = "LangChain", ["llm"] = "LLM",
    };

    // Our skill name  ->  the GitHub search 'language:' qualifier that best finds people who use it.
    private static readonly Dictionary<string, string> ToGitHubLanguage = new(StringComparer.OrdinalIgnoreCase)
    {
        [".NET"] = "C#", ["C#"] = "C#", ["F#"] = "F#",
        ["Angular"] = "TypeScript", ["React"] = "JavaScript", ["TypeScript"] = "TypeScript",
        ["JavaScript"] = "JavaScript", ["Python"] = "Python", ["Java"] = "Java",
        ["Go"] = "Go", ["Rust"] = "Rust",
    };

    public async Task<IReadOnlyList<ExternalCandidate>> SearchAsync(
        Demand demand, CandidateSearchOptions options, CancellationToken ct = default)
    {
        var limit = Math.Clamp(options.Limit, 1, 15);

        // Pick the most distinctive required skill that maps to a GitHub language; default to C#.
        var language = demand.RequiredSkills
            .Select(s => ToGitHubLanguage.GetValueOrDefault(s.Trim()))
            .FirstOrDefault(x => x is not null) ?? "C#";

        var q = $"language:{language} repos:>5";
        if (!string.IsNullOrWhiteSpace(options.Location))
            q += $" location:{options.Location.Trim()}";

        var searchUrl = $"search/users?q={Uri.EscapeDataString(q)}&sort=followers&order=desc&per_page={limit}";

        var search = await GetAsync<SearchResponse>(searchUrl, ct, isSearch: true);
        var logins = search?.Items?.Select(i => i.Login).Where(l => !string.IsNullOrWhiteSpace(l)).ToList()
                     ?? new List<string>();
        if (logins.Count == 0) return Array.Empty<ExternalCandidate>();

        // Enrich each login (profile + repos) concurrently. A single failure skips that candidate,
        // it never sinks the whole batch.
        var tasks = logins.Select(login => EnrichAsync(login, ct));
        var candidates = await Task.WhenAll(tasks);
        return candidates.Where(c => c is not null).Select(c => c!).ToList();
    }

    private async Task<ExternalCandidate?> EnrichAsync(string login, CancellationToken ct)
    {
        try
        {
            var user = await GetAsync<GhUser>($"users/{Uri.EscapeDataString(login)}", ct);
            if (user is null) return null;

            var repos = await GetAsync<List<GhRepo>>(
                $"users/{Uri.EscapeDataString(login)}/repos?sort=pushed&per_page=30", ct) ?? new();

            var skills = InferSkills(repos);
            var ageYears = Math.Max(0, (DateTime.UtcNow - user.CreatedAt).TotalDays / 365.25);
            var seniority = SeniorityHeuristic.Infer(user.PublicRepos, user.Followers, ageYears);

            var headline = Sanitize(user.Bio)
                           ?? $"{user.PublicRepos} public repos · {user.Followers} followers";

            return new ExternalCandidate(
                ExternalId: $"github:{user.Login}",
                Name: string.IsNullOrWhiteSpace(user.Name) ? user.Login : user.Name!,
                Headline: headline,
                ProfileUrl: user.HtmlUrl,
                AvatarUrl: user.AvatarUrl,
                Location: Sanitize(user.Location),
                InferredSeniority: seniority,
                Skills: skills,
                SourceName: "GitHub");
        }
        catch (CandidateSourceException) { throw; }
        catch { return null; } // one bad profile shouldn't kill the search
    }

    /// <summary>Distinct skills from repo languages (mapped or kept raw) + repo topics that map to our vocabulary.</summary>
    private static List<string> InferSkills(List<GhRepo> repos)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var skills = new List<string>();

        void Add(string? value, bool onlyIfCanonical)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            var canonical = ToCanonicalSkill.GetValueOrDefault(value.Trim());
            var name = canonical ?? (onlyIfCanonical ? null : value.Trim());
            if (name is null || !seen.Add(name)) return;
            skills.Add(name);
        }

        foreach (var r in repos.Where(r => !r.Fork))
            Add(r.Language, onlyIfCanonical: false);
        foreach (var r in repos.Where(r => !r.Fork))
            foreach (var t in r.Topics ?? new())
                Add(t, onlyIfCanonical: true); // topics are noisy — keep only the ones we recognize

        return skills;
    }

    /// <summary>
    /// GitHub bios/locations are user-controlled free text that later feeds an LLM prompt. Neutralize the
    /// obvious prompt-injection / layout-breaking characters and cap the length before it travels further.
    /// </summary>
    private static string? Sanitize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var cleaned = raw.Replace("\r", " ").Replace("\n", " ").Replace("`", "'").Trim();
        return cleaned.Length > 160 ? cleaned[..160] + "…" : cleaned;
    }

    private async Task<T?> GetAsync<T>(string url, CancellationToken ct, bool isSearch = false)
    {
        using var res = await _http.GetAsync(url, ct);

        if (res.StatusCode == HttpStatusCode.Forbidden &&
            res.Headers.TryGetValues("X-RateLimit-Remaining", out var rem) && rem.FirstOrDefault() == "0")
        {
            throw new CandidateSourceException(
                "GitHub API rate limit reached. Set a GITHUB_TOKEN to raise it from 60 to 5000 requests/hour.");
        }

        if (!res.IsSuccessStatusCode)
        {
            if (isSearch)
                throw new CandidateSourceException($"GitHub search failed ({(int)res.StatusCode} {res.StatusCode}).");
            return default; // enrichment: tolerate and skip
        }

        var json = await res.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<T>(json, JsonOpts);
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // ---- GitHub API response shapes (only the fields we use) ----
    private sealed class SearchResponse
    {
        [JsonPropertyName("items")] public List<SearchItem>? Items { get; set; }
    }
    private sealed class SearchItem
    {
        [JsonPropertyName("login")] public string Login { get; set; } = string.Empty;
    }
    private sealed class GhUser
    {
        [JsonPropertyName("login")] public string Login { get; set; } = string.Empty;
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("html_url")] public string HtmlUrl { get; set; } = string.Empty;
        [JsonPropertyName("avatar_url")] public string? AvatarUrl { get; set; }
        [JsonPropertyName("bio")] public string? Bio { get; set; }
        [JsonPropertyName("location")] public string? Location { get; set; }
        [JsonPropertyName("public_repos")] public int PublicRepos { get; set; }
        [JsonPropertyName("followers")] public int Followers { get; set; }
        [JsonPropertyName("created_at")] public DateTime CreatedAt { get; set; }
    }
    private sealed class GhRepo
    {
        [JsonPropertyName("language")] public string? Language { get; set; }
        [JsonPropertyName("topics")] public List<string>? Topics { get; set; }
        [JsonPropertyName("fork")] public bool Fork { get; set; }
    }
}
