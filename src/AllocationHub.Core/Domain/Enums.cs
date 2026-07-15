namespace AllocationHub.Core.Domain;

/// <summary>Consultant seniority, ordered so it can be compared against a demand's requirement.</summary>
public enum Seniority
{
    Junior = 0,
    Mid = 1,
    Senior = 2,
    Lead = 3
}

public enum Availability
{
    Available = 0,
    Allocated = 1,
    Unavailable = 2
}

public enum DemandStatus
{
    Open = 0,
    Allocated = 1,
    Closed = 2
}

public enum AllocationStatus
{
    Active = 0,
    Ended = 1
}

public enum UserRole
{
    Admin = 0
}
