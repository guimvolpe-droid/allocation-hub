using AllocationHub.Core.Domain;

namespace AllocationHub.Core.Sourcing;

/// <summary>
/// Deterministic seniority inference from public GitHub signals. Kept pure (no API, no clock inside)
/// so it is unit-tested and explainable: the same inputs always yield the same seniority. The caller
/// passes the account age in years, computed from the profile's creation date.
/// </summary>
public static class SeniorityHeuristic
{
    public static Seniority Infer(int publicRepos, int followers, double accountAgeYears)
    {
        var points = 0;

        if (accountAgeYears >= 8) points += 2;
        else if (accountAgeYears >= 4) points += 1;

        if (publicRepos >= 30) points += 2;
        else if (publicRepos >= 10) points += 1;

        if (followers >= 100) points += 2;
        else if (followers >= 20) points += 1;

        return points switch
        {
            >= 5 => Seniority.Lead,
            >= 3 => Seniority.Senior,
            >= 1 => Seniority.Mid,
            _ => Seniority.Junior
        };
    }
}
