namespace AllocationHub.Core.Ai;

/// <summary>The verdict of validating one LLM-written explanation against the facts.</summary>
public record GuardResult(bool Ok, string Reason)
{
    public static readonly GuardResult Pass = new(true, "ok");
    public static GuardResult Fail(string reason) => new(false, reason);
}

/// <summary>
/// Pure output guardrail + eval for LLM-written match explanations. It is the gate that lets us trust a
/// non-deterministic model in a demo: the explanation is only shown if it stays within bounds and does NOT
/// invent a technology that is neither in the candidate's skills nor the demand's requirements. Being pure,
/// it is unit-tested and runs as a CI quality gate — the same check the runtime applies before display.
/// </summary>
public static class ExplanationGuard
{
    // Known technologies the model might name. If one appears in the output but in neither the candidate's
    // nor the demand's skill set, it's a hallucination and we reject (fall back to the deterministic text).
    private static readonly string[] KnownTech =
    {
        ".NET", "C#", "F#", "Java", "Python", "Go", "Rust", "TypeScript", "JavaScript",
        "Angular", "React", "Vue", "Node", "Kafka", "RabbitMQ", "SQL Server", "PostgreSQL",
        "MySQL", "MongoDB", "Redis", "Azure", "AWS", "GCP", "Docker", "Kubernetes",
        "RAG", "LangChain", "Spring", "Django", "Flask"
    };

    // Cheap prompt-injection canaries: if the model parroted these, the output is compromised.
    private static readonly string[] InjectionCanaries =
    {
        "ignore previous", "ignore the above", "system prompt", "as an ai language model"
    };

    public static GuardResult Validate(
        string? output, IReadOnlyList<string> candidateSkills, IReadOnlyList<string> demandSkills)
    {
        if (string.IsNullOrWhiteSpace(output))
            return GuardResult.Fail("empty output");

        var text = output.Trim();
        if (text.Length < 10) return GuardResult.Fail("too short");
        if (text.Length > 400) return GuardResult.Fail("too long");

        var lower = text.ToLowerInvariant();
        foreach (var canary in InjectionCanaries)
            if (lower.Contains(canary))
                return GuardResult.Fail($"injection canary: '{canary}'");

        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in candidateSkills) allowed.Add(s.Trim());
        foreach (var s in demandSkills) allowed.Add(s.Trim());

        foreach (var tech in KnownTech)
        {
            if (allowed.Contains(tech)) continue;
            if (ContainsToken(lower, tech.ToLowerInvariant()))
                return GuardResult.Fail($"hallucinated skill: '{tech}'");
        }

        return GuardResult.Pass;
    }

    /// <summary>Whole-token contains, so "Go" doesn't match "Google" and ".NET" matches around punctuation.</summary>
    private static bool ContainsToken(string haystack, string token)
    {
        var idx = 0;
        while ((idx = haystack.IndexOf(token, idx, StringComparison.Ordinal)) >= 0)
        {
            var before = idx == 0 || !IsWordChar(haystack[idx - 1]);
            var afterPos = idx + token.Length;
            var after = afterPos >= haystack.Length || !IsWordChar(haystack[afterPos]);
            if (before && after) return true;
            idx = afterPos;
        }
        return false;
    }

    private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '#' || c == '+';
}
