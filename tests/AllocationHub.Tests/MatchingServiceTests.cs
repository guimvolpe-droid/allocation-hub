using AllocationHub.Core.Domain;
using AllocationHub.Core.Matching;
using Xunit;

namespace AllocationHub.Tests;

public class MatchingServiceTests
{
    // The matching rule is pure, so the explanation is irrelevant here — stub it.
    private sealed class NoExplanation : IMatchExplanationService
    {
        public string Explain(Demand demand, Consultant consultant, MatchScore score) => string.Empty;
    }

    private readonly MatchingService _svc = new(new NoExplanation());

    private static Demand DotNetDemand() => new()
    {
        Title = "Senior .NET integration engineer",
        RequiredSeniority = Seniority.Senior,
        RequiredSkills = new() { ".NET", "Kafka", "SQL Server" }
    };

    [Fact]
    public void Available_senior_with_all_skills_scores_100()
    {
        var c = new Consultant
        {
            Seniority = Seniority.Senior, Availability = Availability.Available,
            Skills = new() { ".NET", "Kafka", "SQL Server", "Azure" }
        };

        var score = _svc.Score(DotNetDemand(), c);

        // +50 available, +20 seniority meets, +10*3 skills
        Assert.Equal(100, score.Score);
        Assert.Empty(score.MissingSkills);
        Assert.True(score.MeetsSeniority);
    }

    [Fact]
    public void Missing_skill_is_reported_with_original_casing_and_matching_is_case_insensitive()
    {
        var c = new Consultant
        {
            Seniority = Seniority.Senior, Availability = Availability.Available,
            Skills = new() { ".net", "sql server" } // lower-case on purpose
        };

        var score = _svc.Score(DotNetDemand(), c);

        Assert.Equal(new[] { ".NET", "SQL Server" }, score.MatchedSkills);
        Assert.Equal(new[] { "Kafka" }, score.MissingSkills); // original casing preserved
        Assert.Equal(50 + 20 + 20, score.Score); // available + seniority + 2 skills
    }

    [Fact]
    public void Allocated_consultant_takes_the_penalty()
    {
        var c = new Consultant
        {
            Seniority = Seniority.Lead, Availability = Availability.Allocated,
            Skills = new() { ".NET", "Kafka", "SQL Server" }
        };

        var score = _svc.Score(DotNetDemand(), c);

        // -30 allocated, +20 seniority (Lead >= Senior), +30 skills
        Assert.Equal(20, score.Score);
    }

    [Fact]
    public void Below_required_seniority_gets_no_seniority_bonus()
    {
        var c = new Consultant
        {
            Seniority = Seniority.Junior, Availability = Availability.Available,
            Skills = new() { ".NET" }
        };

        var score = _svc.Score(DotNetDemand(), c);

        Assert.False(score.MeetsSeniority);
        Assert.Equal(50 + 10, score.Score); // available + 1 skill, no seniority bonus
    }

    [Fact]
    public void Rank_orders_by_score_descending()
    {
        var demand = DotNetDemand();
        var strong = new Consultant { Name = "Strong", Seniority = Seniority.Senior,
            Availability = Availability.Available, Skills = new() { ".NET", "Kafka", "SQL Server" } };
        var weak = new Consultant { Name = "Weak", Seniority = Seniority.Junior,
            Availability = Availability.Unavailable, Skills = new() { "PHP" } };

        var ranked = _svc.Rank(demand, new[] { weak, strong });

        Assert.Equal("Strong", ranked[0].Consultant.Name);
        Assert.True(ranked[0].Score.Score > ranked[1].Score.Score);
    }
}
