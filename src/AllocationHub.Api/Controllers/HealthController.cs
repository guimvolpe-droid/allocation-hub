using AllocationHub.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AllocationHub.Api.Controllers;

/// <summary>
/// Proves the database connection at a glance: which EF provider is live, whether it can reach the
/// server, and real row counts. Anonymous on purpose — it's the first thing to check when a deploy or a
/// container can't talk to its database.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _db;

    public HealthController(AppDbContext db) => _db = db;

    public record HealthDto(
        string Status, string Provider, string Database, bool CanConnect,
        int Consultants, int Clients, int Demands, int Allocations);

    [HttpGet]
    public async Task<ActionResult<HealthDto>> Get(CancellationToken ct)
    {
        var canConnect = await _db.Database.CanConnectAsync(ct);
        if (!canConnect)
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new HealthDto("unhealthy", Friendly(_db.Database.ProviderName), "unreachable", false, 0, 0, 0, 0));

        return new HealthDto(
            Status: "healthy",
            Provider: Friendly(_db.Database.ProviderName),
            Database: _db.Database.GetDbConnection().Database,
            CanConnect: true,
            Consultants: await _db.Consultants.CountAsync(ct),
            Clients: await _db.Clients.CountAsync(ct),
            Demands: await _db.Demands.CountAsync(ct),
            Allocations: await _db.Allocations.CountAsync(ct));
    }

    private static string Friendly(string? providerName) => providerName switch
    {
        "Npgsql.EntityFrameworkCore.PostgreSQL" => "PostgreSQL (Supabase)",
        "Microsoft.EntityFrameworkCore.Sqlite" => "SQLite (local file)",
        _ => providerName ?? "unknown"
    };
}
