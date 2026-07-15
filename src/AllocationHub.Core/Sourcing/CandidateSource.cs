using AllocationHub.Core.Domain;

namespace AllocationHub.Core.Sourcing;

/// <summary>
/// A candidate discovered in an external system (GitHub today), normalized to our own vocabulary.
/// The point of this record is that the matching rule never learns where a candidate came from — a
/// LinkedIn or StackOverflow source could produce the same shape without touching <c>MatchingService</c>.
/// </summary>
public record ExternalCandidate(
    string ExternalId,
    string Name,
    string? Headline,
    string ProfileUrl,
    string? AvatarUrl,
    string? Location,
    Seniority InferredSeniority,
    IReadOnlyList<string> Skills,
    string SourceName)
{
    /// <summary>
    /// Projects the external candidate onto a transient <see cref="Consultant"/> so the SAME pure
    /// <c>MatchingService</c> can score it. Id = 0 (never persisted); treated as Available because an
    /// external candidate is not allocated inside this system.
    /// </summary>
    public Consultant ToConsultant() => new()
    {
        Id = 0,
        Name = Name,
        Email = string.Empty,
        Seniority = InferredSeniority,
        Location = Location ?? string.Empty,
        Availability = Availability.Available,
        HourlyRate = 0m,
        Skills = Skills.ToList()
    };
}

/// <summary>Query knobs for an external search. Limit is clamped by the source to a safe ceiling.</summary>
public record CandidateSearchOptions(string? Location = null, int Limit = 6);

/// <summary>
/// A source of candidate profiles. Implementations live in Infrastructure (they talk to the network);
/// the contract lives here in Core so the API and the matching rule depend only on the abstraction.
/// </summary>
public interface ICandidateSource
{
    /// <summary>Stable lower-case identifier used to select the source (e.g. "github").</summary>
    string Name { get; }

    Task<IReadOnlyList<ExternalCandidate>> SearchAsync(
        Demand demand, CandidateSearchOptions options, CancellationToken ct = default);
}

/// <summary>Raised when an external source fails in a way the caller should surface (rate limit, upstream down).</summary>
public class CandidateSourceException : Exception
{
    public CandidateSourceException(string message, Exception? inner = null) : base(message, inner) { }
}
