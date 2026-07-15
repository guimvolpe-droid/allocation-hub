using AllocationHub.Api.Mapping;
using AllocationHub.Core.Domain;
using AllocationHub.Core.Dtos;
using AllocationHub.Core.Matching;
using AllocationHub.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AllocationHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/demands")]
public class DemandsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly MatchingService _matching;

    public DemandsController(AppDbContext db, MatchingService matching)
    {
        _db = db; _matching = matching;
    }

    [HttpGet]
    public async Task<ActionResult<List<DemandDto>>> List() =>
        await _db.Demands.AsNoTracking().Include(d => d.Client)
            .OrderByDescending(d => d.Status == DemandStatus.Open).ThenBy(d => d.Title)
            .Select(d => d.ToDto()).ToListAsync();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DemandDto>> Get(int id)
    {
        var d = await _db.Demands.Include(x => x.Client).FirstOrDefaultAsync(x => x.Id == id);
        return d is null ? NotFound() : d.ToDto();
    }

    [HttpPost]
    public async Task<ActionResult<DemandDto>> Create(DemandRequest req)
    {
        if (!await _db.Clients.AnyAsync(c => c.Id == req.ClientId))
            return BadRequest(new { message = "Client not found." });

        var d = new Demand
        {
            ClientId = req.ClientId, Title = req.Title, Description = req.Description,
            RequiredSeniority = req.RequiredSeniority, RequiredSkills = req.RequiredSkills ?? new(),
            Status = DemandStatus.Open
        };
        _db.Demands.Add(d);
        await _db.SaveChangesAsync();
        await _db.Entry(d).Reference(x => x.Client).LoadAsync();
        return CreatedAtAction(nameof(Get), new { id = d.Id }, d.ToDto());
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<DemandDto>> Update(int id, DemandRequest req)
    {
        var d = await _db.Demands.Include(x => x.Client).FirstOrDefaultAsync(x => x.Id == id);
        if (d is null) return NotFound();
        d.ClientId = req.ClientId; d.Title = req.Title; d.Description = req.Description;
        d.RequiredSeniority = req.RequiredSeniority; d.RequiredSkills = req.RequiredSkills ?? new();
        await _db.SaveChangesAsync();
        return d.ToDto();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var d = await _db.Demands.FindAsync(id);
        if (d is null) return NotFound();
        _db.Demands.Remove(d);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>The recommendation: rank every consultant for this demand with an explainable score.</summary>
    [HttpGet("{id:int}/matches")]
    public async Task<ActionResult<List<MatchDto>>> Matches(int id)
    {
        var demand = await _db.Demands.FindAsync(id);
        if (demand is null) return NotFound();

        var consultants = await _db.Consultants.AsNoTracking().ToListAsync();
        var ranked = _matching.Rank(demand, consultants);

        return ranked.Select(r => new MatchDto(
            r.Consultant.Id, r.Consultant.Name, r.Consultant.Seniority, r.Consultant.Availability,
            r.Score.Score, r.Score.MatchedSkills, r.Score.MissingSkills, r.Explanation)).ToList();
    }
}
