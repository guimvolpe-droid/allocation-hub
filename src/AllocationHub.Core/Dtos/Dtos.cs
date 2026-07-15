using AllocationHub.Core.Domain;

namespace AllocationHub.Core.Dtos;

// ---- Auth ----
public record LoginRequest(string Email, string Password);
public record UserDto(int Id, string Name, string Email, UserRole Role);
public record AuthResponse(string Token, UserDto User);

// ---- Consultants ----
public record ConsultantDto(
    int Id, string Name, string Email, Seniority Seniority, string Location,
    Availability Availability, decimal HourlyRate, List<string> Skills);

public record ConsultantRequest(
    string Name, string Email, Seniority Seniority, string Location,
    Availability Availability, decimal HourlyRate, List<string> Skills);

// ---- Clients ----
public record ClientDto(int Id, string Name, string Industry, string ContactName, int OpenDemands);
public record ClientRequest(string Name, string Industry, string ContactName);

// ---- Demands ----
public record DemandDto(
    int Id, int ClientId, string ClientName, string Title, string Description,
    Seniority RequiredSeniority, List<string> RequiredSkills, DemandStatus Status);

public record DemandRequest(
    int ClientId, string Title, string Description,
    Seniority RequiredSeniority, List<string> RequiredSkills);

// ---- Allocations ----
public record AllocationDto(
    int Id, int DemandId, string DemandTitle, int ConsultantId, string ConsultantName,
    DateOnly StartDate, DateOnly? EndDate, AllocationStatus Status);

public record AllocationRequest(int DemandId, int ConsultantId, DateOnly StartDate);

// ---- Matching ----
public record MatchDto(
    int ConsultantId, string Name, Seniority Seniority, Availability Availability,
    int Score, IReadOnlyList<string> MatchedSkills, IReadOnlyList<string> MissingSkills, string Explanation);

// ---- External sourcing (GitHub) ----
public record ExternalMatchDto(
    string ExternalId, string Name, string? Headline, string ProfileUrl, string? AvatarUrl,
    string? Location, Seniority Seniority, int Score,
    IReadOnlyList<string> MatchedSkills, IReadOnlyList<string> MissingSkills,
    IReadOnlyList<string> Skills, string Explanation, string Source);

// ---- Admin: matching settings ----
public record MatchingSettingsDto(
    int AvailabilityWeight, int SkillWeight, int SeniorityWeight, int AllocatedPenalty,
    DateTime updatedAt, string updatedBy);

public record MatchingSettingsRequest(
    int AvailabilityWeight, int SkillWeight, int SeniorityWeight, int AllocatedPenalty);

// ---- Admin: audit ----
public record AuditLogDto(int Id, string Actor, string Action, string Entity, string Details, DateTime CreatedAt);

// ---- Dashboard ----
public record DashboardSummary(
    int TotalConsultants, int AvailableConsultants, int AllocatedConsultants, int OpenDemands,
    List<DemandDto> TopOpenDemands, List<ConsultantDto> AvailableNow);
