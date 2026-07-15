using AllocationHub.Core.Domain;

namespace AllocationHub.Infrastructure.Data;

/// <summary>Writes an audit entry. Deliberately tiny — an admin trail, not an event store.</summary>
public class AuditWriter
{
    private readonly AppDbContext _db;
    public AuditWriter(AppDbContext db) => _db = db;

    public async Task RecordAsync(string actor, string action, string entity, string details)
    {
        _db.AuditLogs.Add(new AuditLog { Actor = actor, Action = action, Entity = entity, Details = details });
        await _db.SaveChangesAsync();
    }
}
