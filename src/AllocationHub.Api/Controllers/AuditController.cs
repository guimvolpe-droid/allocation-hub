using AllocationHub.Core.Dtos;
using AllocationHub.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AllocationHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/audit")]
public class AuditController : ControllerBase
{
    private readonly AppDbContext _db;
    public AuditController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<AuditLogDto>>> Recent() =>
        await _db.AuditLogs.AsNoTracking().OrderByDescending(a => a.CreatedAt).Take(50)
            .Select(a => new AuditLogDto(a.Id, a.Actor, a.Action, a.Entity, a.Details, a.CreatedAt))
            .ToListAsync();
}
