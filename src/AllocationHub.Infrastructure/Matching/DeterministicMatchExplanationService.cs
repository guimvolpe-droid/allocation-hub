using AllocationHub.Core.Domain;
using AllocationHub.Core.Matching;

namespace AllocationHub.Infrastructure.Matching;

/// <summary>
/// Default explanation: a plain-language sentence derived ENTIRELY from the deterministic score.
/// No external calls — the demo never depends on a model being up. It is the honest baseline the
/// (optional) LLM variant only rephrases.
/// </summary>
public class DeterministicMatchExplanationService : IMatchExplanationService
{
    public string Explain(Demand demand, Consultant consultant, MatchScore score)
    {
        var required = demand.RequiredSkills.Count;
        var fit = required == 0 ? "No specific skills required"
            : score.MatchedSkills.Count == required ? "Full technical fit"
            : score.MatchedSkills.Count == 0 ? "No matching skills"
            : "Partial technical fit";

        var seniority = score.MeetsSeniority ? "seniority meets the requirement" : "below the required seniority";

        var avail = consultant.Availability switch
        {
            Availability.Available => "available now",
            Availability.Allocated => "currently allocated",
            _ => "unavailable"
        };

        var sentence = $"{fit}, {seniority}, and {avail}.";
        if (score.MissingSkills.Count > 0)
            sentence += $" Missing: {string.Join(", ", score.MissingSkills)}.";
        return sentence;
    }
}
