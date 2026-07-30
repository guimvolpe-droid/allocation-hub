using AllocationHub.Core.Ai;
using AllocationHub.Core.Domain;
using AllocationHub.Core.Matching;
using Microsoft.Extensions.Logging;

namespace AllocationHub.Infrastructure.Ai;

/// <summary>
/// Rewrites the deterministic explanation into natural prose using a selected LLM provider — and only
/// keeps the result if it passes <see cref="ExplanationGuard"/>. The prompt is built from STRUCTURED facts
/// (skills, seniority, score), never from the candidate's free-text bio, which is itself an injection
/// defense. Any failure returns the baseline, so the ranking is never blocked or degraded.
/// </summary>
public class LlmExplanationService : ILlmExplanationService
{
    private const string System =
        "You write ONE short, natural sentence (max 35 words) explaining why a consultant fits a staffing " +
        "demand. Use ONLY the facts provided. Use the exact skill names given. Never invent skills, numbers " +
        "or seniority. No preamble, no lists — just the sentence.";

    private readonly ILlmProviderRegistry _registry;
    private readonly ILogger<LlmExplanationService> _log;

    public LlmExplanationService(ILlmProviderRegistry registry, ILogger<LlmExplanationService> log)
    {
        _registry = registry; _log = log;
    }

    public async Task<string> ExplainAsync(
        Demand demand, Consultant consultant, MatchScore score, string baseline,
        string? provider, CancellationToken ct = default)
    {
        var client = _registry.Resolve(provider);
        if (client is null) return baseline; // nothing usable -> deterministic

        try
        {
            var user =
                $"Demand: {demand.Title}\n" +
                $"Required seniority: {demand.RequiredSeniority}\n" +
                $"Required skills: {Join(demand.RequiredSkills)}\n" +
                $"Candidate seniority: {consultant.Seniority} (meets requirement: {score.MeetsSeniority})\n" +
                $"Candidate availability: {consultant.Availability}\n" +
                $"Matched skills: {Join(score.MatchedSkills)}\n" +
                $"Missing skills: {Join(score.MissingSkills)}\n" +
                $"Transparent score: {score.Score}";

            var output = await client.CompleteAsync(System, user, ct);

            var verdict = ExplanationGuard.Validate(output, consultant.Skills, demand.RequiredSkills);
            if (!verdict.Ok)
            {
                _log.LogWarning("LLM explanation rejected by guard ({Reason}); using deterministic baseline.", verdict.Reason);
                return baseline;
            }
            return output;
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "LLM explanation failed ({Provider}); using deterministic baseline.", provider ?? "default");
            return baseline;
        }
    }

    private static string Join(IReadOnlyList<string> items) => items.Count == 0 ? "(none)" : string.Join(", ", items);
}
