using AllocationHub.Core.Domain;
using AllocationHub.Core.Matching;

namespace AllocationHub.Core.Ai;

/// <summary>
/// On-demand, async enrichment of a match explanation by an LLM. The deterministic baseline is always
/// computed first (fast, offline); this only rewrites it into more natural prose when a provider is
/// requested and available. Any failure — provider down, timeout, or a guardrail rejection — returns the
/// baseline unchanged, so the feature can never degrade the result or block the ranking.
/// </summary>
public interface ILlmExplanationService
{
    /// <summary>
    /// Returns an LLM-rewritten explanation, or <paramref name="baseline"/> if enrichment is unavailable
    /// or the output fails the <see cref="ExplanationGuard"/>.
    /// </summary>
    Task<string> ExplainAsync(
        Demand demand, Consultant consultant, MatchScore score, string baseline,
        string? provider, CancellationToken ct = default);
}
