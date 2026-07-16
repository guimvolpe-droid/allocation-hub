using System.Security.Claims;
using AllocationHub.Api.Mapping;
using AllocationHub.Core.Domain;
using AllocationHub.Core.Dtos;
using AllocationHub.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AllocationHub.Api.Controllers;

/// <summary>Payload for importing an externally sourced candidate (e.g. GitHub) as a consultant.</summary>
public record ImportConsultantRequest(
    string ExternalId, string Name, Seniority Seniority, string? Location,
    List<string> Skills, string? ProfileUrl, string Source);

[ApiController]
[Authorize]
[Route("api/consultants")]
public class ConsultantsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AuditWriter _audit;

    public ConsultantsController(AppDbContext db, AuditWriter audit) { _db = db; _audit = audit; }

    [HttpGet]
    public async Task<ActionResult<List<ConsultantDto>>> List(
        [FromQuery] Seniority? seniority, [FromQuery] Availability? availability, [FromQuery] string? skill)
    {
        var consultants = await _db.Consultants.AsNoTracking().ToListAsync();

        IEnumerable<Consultant> q = consultants;
        if (seniority is not null) q = q.Where(c => c.Seniority == seniority);
        if (availability is not null) q = q.Where(c => c.Availability == availability);
        if (!string.IsNullOrWhiteSpace(skill))
            q = q.Where(c => c.Skills.Any(s => s.Contains(skill, StringComparison.OrdinalIgnoreCase)));

        return q.OrderBy(c => c.Name).Select(c => c.ToDto()).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ConsultantDto>> Get(int id)
    {
        var c = await _db.Consultants.FindAsync(id);
        return c is null ? NotFound() : c.ToDto();
    }

    [HttpPost]
    public async Task<ActionResult<ConsultantDto>> Create(ConsultantRequest req)
    {
        var c = new Consultant();
        req.Apply(c);
        _db.Consultants.Add(c);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = c.Id }, c.ToDto());
    }

    [HttpPost("import")]
    public async Task<ActionResult<ConsultantDto>> Import(ImportConsultantRequest req)
    {
        // Synthetic email derived from the external login makes the import idempotent.
        var sep = req.ExternalId.IndexOf(':');
        var login = sep >= 0 ? req.ExternalId[(sep + 1)..] : req.ExternalId;
        var email = $"{login}@github.import".ToLowerInvariant();

        var existing = await _db.Consultants.FirstOrDefaultAsync(c => c.Email == email);
        if (existing is not null) return existing.ToDto();

        var c = new Consultant
        {
            Name = req.Name,
            Email = email,
            Seniority = req.Seniority,
            Location = req.Location ?? string.Empty,
            Availability = Availability.Available,
            HourlyRate = 0,
            Skills = req.Skills ?? new(),
        };
        _db.Consultants.Add(c);
        await _db.SaveChangesAsync();

        var actor = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(ClaimTypes.Email) ?? "admin";
        await _audit.RecordAsync(actor, "Import", "Consultant",
            $"externalId={req.ExternalId}, source={req.Source}, profile={req.ProfileUrl}, email={email}");

        return CreatedAtAction(nameof(Get), new { id = c.Id }, c.ToDto());
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ConsultantDto>> Update(int id, ConsultantRequest req)
    {
        var c = await _db.Consultants.FindAsync(id);
        if (c is null) return NotFound();
        req.Apply(c);
        await _db.SaveChangesAsync();
        return c.ToDto();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await _db.Consultants.FindAsync(id);
        if (c is null) return NotFound();
        _db.Consultants.Remove(c);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
