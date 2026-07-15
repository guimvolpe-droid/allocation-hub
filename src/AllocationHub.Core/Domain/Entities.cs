namespace AllocationHub.Core.Domain;

/// <summary>An authenticated back-office user. MVP has a single seeded Admin.</summary>
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Admin;
}

/// <summary>A person who can be allocated to client demands.</summary>
public class Consultant
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Seniority Seniority { get; set; }
    public string Location { get; set; } = string.Empty;
    public Availability Availability { get; set; } = Availability.Available;
    public decimal HourlyRate { get; set; }

    /// <summary>Normalized skill names (e.g. ".NET", "Angular"). No separate Skill table in the MVP.</summary>
    public List<string> Skills { get; set; } = new();
}

/// <summary>A company that opens demands.</summary>
public class Client
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;

    public List<Demand> Demands { get; set; } = new();
}

/// <summary>A staffing request from a client: what skills/seniority it needs.</summary>
public class Demand
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public Client? Client { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Seniority RequiredSeniority { get; set; }
    public List<string> RequiredSkills { get; set; } = new();
    public DemandStatus Status { get; set; } = DemandStatus.Open;
}

/// <summary>A consultant placed on a demand for a period.</summary>
public class Allocation
{
    public int Id { get; set; }
    public int DemandId { get; set; }
    public Demand? Demand { get; set; }
    public int ConsultantId { get; set; }
    public Consultant? Consultant { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public AllocationStatus Status { get; set; } = AllocationStatus.Active;
}
