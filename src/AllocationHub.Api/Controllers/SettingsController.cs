using System.Security.Claims;
using AllocationHub.Core.Dtos;
using AllocationHub.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AllocationHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/settings")]
public class SettingsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AuditWriter _audit;

    public SettingsController(AppDbContext db, AuditWriter audit) { _db = db; _audit = audit; }

    [HttpGet("matching")]
    public async Task<ActionResult<MatchingSettingsDto>> GetMatching()
    {
        var s = await _db.MatchingSettings.FirstOrDefaultAsync();
        if (s is null) return NotFound();
        return new MatchingSettingsDto(s.AvailabilityWeight, s.SkillWeight, s.SeniorityWeight,
            s.AllocatedPenalty, s.UpdatedAt, s.UpdatedBy);
    }

    [HttpPut("matching")]
    public async Task<ActionResult<MatchingSettingsDto>> UpdateMatching(MatchingSettingsRequest req)
    {
        var s = await _db.MatchingSettings.FirstOrDefaultAsync();
        if (s is null) return NotFound();

        var before = $"avail={s.AvailabilityWeight}, skill={s.SkillWeight}, seniority={s.SeniorityWeight}, penalty={s.AllocatedPenalty}";
        s.AvailabilityWeight = req.AvailabilityWeight;
        s.SkillWeight = req.SkillWeight;
        s.SeniorityWeight = req.SeniorityWeight;
        s.AllocatedPenalty = req.AllocatedPenalty;
        s.UpdatedAt = DateTime.UtcNow;
        s.UpdatedBy = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(ClaimTypes.Email) ?? "admin";
        await _db.SaveChangesAsync();

        var after = $"avail={s.AvailabilityWeight}, skill={s.SkillWeight}, seniority={s.SeniorityWeight}, penalty={s.AllocatedPenalty}";
        await _audit.RecordAsync(s.UpdatedBy, "Update", "MatchingSettings", $"{before} → {after}");

        return new MatchingSettingsDto(s.AvailabilityWeight, s.SkillWeight, s.SeniorityWeight,
            s.AllocatedPenalty, s.UpdatedAt, s.UpdatedBy);
    }
}
