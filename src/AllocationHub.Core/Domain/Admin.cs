using AllocationHub.Core.Matching;

namespace AllocationHub.Core.Domain;

/// <summary>
/// The single active configuration of the matching algorithm's weights, administered from the UI.
/// One row; edited by an admin. Converts to the pure <see cref="MatchingWeights"/> the rule consumes.
/// </summary>
public class MatchingSettings
{
    public int Id { get; set; }
    public int AvailabilityWeight { get; set; } = 50;
    public int SkillWeight { get; set; } = 10;
    public int SeniorityWeight { get; set; } = 20;
    public int AllocatedPenalty { get; set; } = -30;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string UpdatedBy { get; set; } = "system";

    public MatchingWeights ToWeights() =>
        new(AvailabilityWeight, SkillWeight, SeniorityWeight, AllocatedPenalty);
}

/// <summary>A minimal audit trail: who changed what, when. Critical admin decisions leave a trace.</summary>
public class AuditLog
{
    public int Id { get; set; }
    public string Actor { get; set; } = string.Empty;   // user name/email
    public string Action { get; set; } = string.Empty;  // e.g. "Update", "Delete"
    public string Entity { get; set; } = string.Empty;  // e.g. "MatchingSettings", "Consultant"
    public string Details { get; set; } = string.Empty; // short before→after summary
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
