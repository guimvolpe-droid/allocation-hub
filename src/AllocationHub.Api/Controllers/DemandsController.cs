using AllocationHub.Api.Mapping;
using AllocationHub.Core.Domain;
using AllocationHub.Core.Dtos;
using AllocationHub.Core.Matching;
using AllocationHub.Core.Sourcing;
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
    private readonly IMatchExplanationService _explanation;
    private readonly IEnumerable<ICandidateSource> _sources;

    public DemandsController(
        AppDbContext db, MatchingService matching,
        IMatchExplanationService explanation, IEnumerable<ICandidateSource> sources)
    {
        _db = db; _matching = matching; _explanation = explanation; _sources = sources;
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

        // Weights are administered from the Settings screen; the pure rule receives them as input.
        var settings = await _db.MatchingSettings.AsNoTracking().FirstOrDefaultAsync();
        var weights = settings?.ToWeights() ?? Core.Matching.MatchingWeights.Default;

        var consultants = await _db.Consultants.AsNoTracking().ToListAsync();
        var ranked = _matching.Rank(demand, consultants, weights);

        return ranked.Select(r => new MatchDto(
            r.Consultant.Id, r.Consultant.Name, r.Consultant.Seniority, r.Consultant.Availability,
            r.Score.Score, r.Score.MatchedSkills, r.Score.MissingSkills, r.Explanation)).ToList();
    }

    /// <summary>
    /// The "spice": rank REAL external candidates (GitHub today) for this demand, using the SAME pure
    /// matching rule and the SAME explanation service. The source is selected by name so LinkedIn or any
    /// provider could plug in without changing this endpoint.
    /// </summary>
    [HttpGet("{id:int}/external-matches")]
    public async Task<ActionResult<IReadOnlyList<ExternalMatchDto>>> ExternalMatches(
        int id, [FromQuery] string source = "github", [FromQuery] string? location = null,
        [FromQuery] int limit = 6, CancellationToken ct = default)
    {
        var demand = await _db.Demands.FindAsync(new object?[] { id }, ct);
        if (demand is null) return NotFound();

        var src = _sources.FirstOrDefault(s => s.Name.Equals(source, StringComparison.OrdinalIgnoreCase));
        if (src is null) return BadRequest(new { message = $"Unknown candidate source '{source}'." });

        var settings = await _db.MatchingSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        var weights = settings?.ToWeights() ?? MatchingWeights.Default;

        IReadOnlyList<ExternalCandidate> candidates;
        try
        {
            candidates = await src.SearchAsync(demand, new CandidateSearchOptions(location, limit), ct);
        }
        catch (CandidateSourceException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message });
        }

        return candidates
            .Select(cand =>
            {
                var consultant = cand.ToConsultant();
                var score = _matching.Score(demand, consultant, weights);
                var explanation = _explanation.Explain(demand, consultant, score);
                return new ExternalMatchDto(
                    cand.ExternalId, cand.Name, cand.Headline, cand.ProfileUrl, cand.AvatarUrl,
                    cand.Location, cand.InferredSeniority, score.Score,
                    score.MatchedSkills, score.MissingSkills, cand.Skills, explanation, cand.SourceName);
            })
            .OrderByDescending(r => r.Score)
            .ToList();
    }
}
