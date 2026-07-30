using AllocationHub.Core.Domain;
using AllocationHub.Core.Matching;
using AllocationHub.Core.Sourcing;
using Xunit;

namespace AllocationHub.Tests;

/// <summary>
/// The external-sourcing glue that must stay correct without a network: the seniority heuristic and the
/// projection of an external candidate onto a transient Consultant so the SAME matching rule can score it.
/// </summary>
public class SourcingTests
{
    [Theory]
    [InlineData(0, 0, 0.5, Seniority.Junior)]     // brand-new account, nothing yet
    [InlineData(12, 25, 5, Seniority.Senior)]     // 4y+ (1) + 10+ repos (1) + 20+ followers (1) = 3
    [InlineData(40, 200, 10, Seniority.Lead)]     // 8y+ (2) + 30+ repos (2) + 100+ followers (2) = 6
    [InlineData(10, 5, 1, Seniority.Mid)]         // 10+ repos (1) only = 1
    public void Seniority_is_inferred_deterministically(int repos, int followers, double ageYears, Seniority expected)
    {
        Assert.Equal(expected, SeniorityHeuristic.Infer(repos, followers, ageYears));
    }

    [Fact]
    public void External_candidate_projects_onto_a_transient_available_consultant()
    {
        var cand = new ExternalCandidate(
            ExternalId: "github:octocat", Name: "The Octocat", Headline: "builds things",
            ProfileUrl: "https://github.com/octocat", AvatarUrl: null, Location: "Sao Paulo",
            InferredSeniority: Seniority.Senior, Skills: new[] { ".NET", "Kafka" }, SourceName: "GitHub");

        var c = cand.ToConsultant();

        Assert.Equal(0, c.Id);                              // transient, never persisted
        Assert.Equal(Availability.Available, c.Availability); // an external candidate isn't allocated here
        Assert.Equal(Seniority.Senior, c.Seniority);
        Assert.Equal(new[] { ".NET", "Kafka" }, c.Skills);
        Assert.Equal("Sao Paulo", c.Location);
    }

    [Fact]
    public void A_projected_candidate_scores_through_the_same_matching_rule()
    {
        var demand = new Demand
        {
            Title = "Senior .NET integration engineer",
            RequiredSeniority = Seniority.Senior,
            RequiredSkills = new() { ".NET", "Kafka", "SQL Server" }
        };
        var cand = new ExternalCandidate(
            "github:dev", "Dev Real", null, "https://github.com/dev", null, null,
            Seniority.Senior, new[] { ".NET", "Kafka", "SQL Server" }, "GitHub");

        var svc = new MatchingService(new PassThroughExplanation());
        var score = svc.Score(demand, cand.ToConsultant(), MatchingWeights.Default);

        // +50 available, +20 seniority, +30 three skills
        Assert.Equal(100, score.Score);
        Assert.Empty(score.MissingSkills);
    }

    private sealed class PassThroughExplanation : IMatchExplanationService
    {
        public string Explain(Demand demand, Consultant consultant, MatchScore score) => string.Empty;
    }
}
