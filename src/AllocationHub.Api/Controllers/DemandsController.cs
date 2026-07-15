using AllocationHub.Api.Mapping;
using AllocationHub.Core.Ai;
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
    private readonly ILlmExplanationService _llm;
    private readonly IEnumerable<ICandidateSource> _sources;

    public DemandsController(
        AppDbContext db, MatchingService matching, IMatchExplanationService explanation,
        ILlmExplanationService llm, IEnumerable<ICandidateSource> sources)
    {
        _db = db; _matching = matching; _explanation = explanation; _llm = llm; _sources = sources;
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

    /// <summary>
    /// The recommendation: rank every consultant for this demand with an explainable score. The
    /// deterministic explanation is always computed; pass <paramref name="provider"/> (e.g. "groq") to have
    /// that LLM rewrite each explanation — guarded, with fallback to the deterministic text.
    /// </summary>
    [HttpGet("{id:int}/matches")]
    public async Task<ActionResult<List<MatchDto>>> Matches(
        int id, [FromQuery] string? provider = null, CancellationToken ct = default)
    {
        var demand = await _db.Demands.FindAsync(new object?[] { id }, ct);
        if (demand is null) return NotFound();

        // Weights are administered from the Settings screen; the pure rule receives them as input.
        var settings = await _db.MatchingSettings.AsNoTracking().FirstOrDefaultAsync(ct);
        var weights = settings?.ToWeights() ?? MatchingWeights.Default;

        var consultants = await _db.Consultants.AsNoTracking().ToListAsync(ct);
        var ranked = _matching.Rank(demand, consultants, weights);

        var explanations = await EnrichAsync(
            demand, ranked.Select(r => (r.Consultant, r.Score, r.Explanation)), provider, ct);

        return ranked.Select((r, i) => new MatchDto(
            r.Consultant.Id, r.Consultant.Name, r.Consultant.Seniority, r.Consultant.Availability,
            r.Score.Score, r.Score.MatchedSkills, r.Score.MissingSkills, explanations[i])).ToList();
    }

    /// <summary>
    /// Turns baseline explanations into LLM-written ones when a provider is requested — in parallel, and
    /// each one guarded (bad or hallucinated output falls back to its deterministic baseline).
    /// </summary>
    private async Task<List<string>> EnrichAsync(
        Demand demand, IEnumerable<(Consultant c, MatchScore s, string baseline)> items,
        string? provider, CancellationToken ct)
    {
        var list = items.ToList();
        if (string.IsNullOrWhiteSpace(provider))
            return list.Select(x => x.baseline).ToList();

        var tasks = list.Select(x => _llm.ExplainAsync(demand, x.c, x.s, x.baseline, provider, ct));
        return (await Task.WhenAll(tasks)).ToList();
    }

    /// <summary>
    /// The "spice": rank REAL external candidates (GitHub today) for this demand, using the SAME pure
    /// matching rule and the SAME explanation service. The source is selected by name so LinkedIn or any
    /// provider could plug in without changing this endpoint.
    /// </summary>
    [HttpGet("{id:int}/external-matches")]
    public async Task<ActionResult<IReadOnlyList<ExternalMatchDto>>> ExternalMatches(
        int id, [FromQuery] string source = "github", [FromQuery] string? location = null,
        [FromQuery] int limit = 6, [FromQuery] string? provider = null, CancellationToken ct = default)
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

        var scored = candidates.Select(cand =>
        {
            var consultant = cand.ToConsultant();
            var score = _matching.Score(demand, consultant, weights);
            var baseline = _explanation.Explain(demand, consultant, score);
            return (cand, consultant, score, baseline);
        }).ToList();

        var explanations = await EnrichAsync(
            demand, scored.Select(x => (x.consultant, x.score, x.baseline)), provider, ct);

        return scored.Select((x, i) => new ExternalMatchDto(
                x.cand.ExternalId, x.cand.Name, x.cand.Headline, x.cand.ProfileUrl, x.cand.AvatarUrl,
                x.cand.Location, x.cand.InferredSeniority, x.score.Score,
                x.score.MatchedSkills, x.score.MissingSkills, x.cand.Skills, explanations[i], x.cand.SourceName))
            .OrderByDescending(r => r.Score)
            .ToList();
    }
}
