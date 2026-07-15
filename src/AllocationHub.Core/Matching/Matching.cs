using AllocationHub.Core.Domain;

namespace AllocationHub.Core.Matching;

/// <summary>The transparent result of scoring one consultant against one demand.</summary>
public record MatchScore(
    int Score,
    IReadOnlyList<string> MatchedSkills,
    IReadOnlyList<string> MissingSkills,
    bool MeetsSeniority);

/// <summary>A consultant ranked for a demand, with a human-readable explanation.</summary>
public record MatchResult(
    Consultant Consultant,
    MatchScore Score,
    string Explanation);

/// <summary>
/// Turns a deterministic <see cref="MatchScore"/> into a human-readable sentence.
/// Kept behind an interface so the explanation can be produced deterministically (default) or by an
/// LLM (optional) WITHOUT the scoring rule ever depending on an external vendor.
/// </summary>
public interface IMatchExplanationService
{
    string Explain(Demand demand, Consultant consultant, MatchScore score);
}

/// <summary>
/// The tunable weights of the matching rule. Defaults live here; the running values are administered
/// from the Settings screen and persisted, then passed into the (still pure) rule. Keeping the weights
/// as a value object is what lets the operation reconfigure the algorithm without a code change.
/// </summary>
public record MatchingWeights(
    int AvailabilityBonus,
    int SkillPoints,
    int SeniorityBonus,
    int AllocatedPenalty)
{
    public static readonly MatchingWeights Default = new(
        AvailabilityBonus: 50, SkillPoints: 10, SeniorityBonus: 20, AllocatedPenalty: -30);
}

/// <summary>
/// Pure, framework- and database-free matching rule. This is the business core of the app and the
/// only thing that must be unit-tested: given a demand, candidates and weights, it produces an
/// explainable, ranked recommendation. See docs/matching.md for the formula.
/// </summary>
public class MatchingService
{
    private readonly IMatchExplanationService _explanation;

    public MatchingService(IMatchExplanationService explanation) => _explanation = explanation;

    /// <summary>Scores a single consultant against a demand. Deterministic and side-effect free.</summary>
    public MatchScore Score(Demand demand, Consultant consultant, MatchingWeights weights)
    {
        var owned = Normalize(consultant.Skills);

        // Classify the ORIGINAL required skills (keep their casing for display), matching case-insensitively.
        var required = demand.RequiredSkills.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        var matched = required.Where(s => owned.Contains(s.Trim().ToLowerInvariant())).ToList();
        var missing = required.Where(s => !owned.Contains(s.Trim().ToLowerInvariant())).ToList();

        var meetsSeniority = consultant.Seniority >= demand.RequiredSeniority;

        var score = 0;
        if (consultant.Availability == Availability.Available) score += weights.AvailabilityBonus;
        if (consultant.Availability == Availability.Allocated) score += weights.AllocatedPenalty;
        if (meetsSeniority) score += weights.SeniorityBonus;
        score += matched.Count * weights.SkillPoints;

        return new MatchScore(score, matched, missing, meetsSeniority);
    }

    /// <summary>Ranks all candidates for a demand, best first. Ties broken by seniority then name.</summary>
    public IReadOnlyList<MatchResult> Rank(Demand demand, IEnumerable<Consultant> candidates, MatchingWeights weights)
    {
        return candidates
            .Select(c =>
            {
                var score = Score(demand, c, weights);
                return new MatchResult(c, score, _explanation.Explain(demand, c, score));
            })
            .OrderByDescending(r => r.Score.Score)
            .ThenByDescending(r => r.Consultant.Seniority)
            .ThenBy(r => r.Consultant.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static HashSet<string> Normalize(IEnumerable<string> skills) =>
        skills.Select(s => s.Trim().ToLowerInvariant())
              .Where(s => s.Length > 0)
              .ToHashSet();
}
