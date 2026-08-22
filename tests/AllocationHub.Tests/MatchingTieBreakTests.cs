using AllocationHub.Core.Domain;
using AllocationHub.Core.Matching;
using Xunit;

namespace AllocationHub.Tests;

/// <summary>
/// Edge cases of the "deterministic and explainable" promise: ties, normalization and the
/// neutral availability state. A rule that reorders equals between runs, or scores a skill
/// differently because of spacing, is no longer explainable — these tests pin that down.
/// </summary>
public class MatchingTieBreakTests
{
    private sealed class NoExplanation : IMatchExplanationService
    {
        public string Explain(Demand demand, Consultant consultant, MatchScore score) => string.Empty;
    }

    private readonly MatchingService _svc = new(new NoExplanation());
    private readonly MatchingWeights W = MatchingWeights.Default;

    private static Demand Demand() => new()
    {
        Title = "Any",
        RequiredSeniority = Seniority.Senior,
        RequiredSkills = new() { ".NET" }
    };

    private static Consultant C(string name, Seniority s = Seniority.Senior) => new()
    {
        Name = name, Seniority = s, Availability = Availability.Available, Skills = new() { ".NET" }
    };

    [Fact]
    public void Equal_scores_break_by_seniority_then_name_case_insensitively()
    {
        // Lead and Senior both meet the bar → same score; Lead must come first.
        var lead = C("Zoe", Seniority.Lead);
        var seniorA = C("ana");
        var seniorB = C("Bruno");

        var ranked = _svc.Rank(Demand(), new[] { seniorB, seniorA, lead }, W);

        Assert.Equal(ranked[0].Score.Score, ranked[1].Score.Score); // é empate de verdade
        Assert.Equal("Zoe", ranked[0].Consultant.Name);             // seniority desempata primeiro
        Assert.Equal("ana", ranked[1].Consultant.Name);             // depois nome, sem caixa
        Assert.Equal("Bruno", ranked[2].Consultant.Name);
    }

    [Fact]
    public void Rank_is_stable_for_identical_inputs_regardless_of_input_order()
    {
        var a = C("ana");
        var b = C("Bruno");
        var first = _svc.Rank(Demand(), new[] { a, b }, W);
        var second = _svc.Rank(Demand(), new[] { b, a }, W);
        Assert.Equal(first[0].Consultant.Name, second[0].Consultant.Name);
        Assert.Equal(first[1].Consultant.Name, second[1].Consultant.Name);
    }

    [Fact]
    public void Skill_matching_ignores_surrounding_whitespace_on_both_sides()
    {
        var demand = new Demand
        {
            Title = "Any", RequiredSeniority = Seniority.Junior,
            RequiredSkills = new() { "  Kafka  " }
        };
        var c = new Consultant
        {
            Seniority = Seniority.Junior, Availability = Availability.Available,
            Skills = new() { " kafka " }
        };

        var score = _svc.Score(demand, c, W);

        Assert.Single(score.MatchedSkills);
        Assert.Empty(score.MissingSkills);
    }

    [Fact]
    public void Blank_required_skills_are_ignored_not_counted_as_missing()
    {
        var demand = new Demand
        {
            Title = "Any", RequiredSeniority = Seniority.Junior,
            RequiredSkills = new() { "", "   ", ".NET" }
        };
        var c = new Consultant
        {
            Seniority = Seniority.Junior, Availability = Availability.Available,
            Skills = new() { ".net" }
        };

        var score = _svc.Score(demand, c, W);

        Assert.Empty(score.MissingSkills); // os vazios não viram "faltando"
        Assert.Equal(50 + 20 + 10, score.Score);
    }

    [Fact]
    public void Unavailable_is_neutral_no_bonus_and_no_penalty()
    {
        var c = new Consultant
        {
            Seniority = Seniority.Senior, Availability = Availability.Unavailable,
            Skills = new() { ".NET" }
        };

        var score = _svc.Score(Demand(), c, W);

        Assert.Equal(20 + 10, score.Score); // só seniority + skill: sem os ±. do availability
    }

    [Fact]
    public void Empty_candidate_list_ranks_to_empty_never_throws()
    {
        var ranked = _svc.Rank(Demand(), System.Array.Empty<Consultant>(), W);
        Assert.Empty(ranked);
    }
}
