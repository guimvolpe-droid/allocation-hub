using AllocationHub.Core.Domain;
using AllocationHub.Core.Dtos;

namespace AllocationHub.Api.Mapping;

/// <summary>Hand-written entity ↔ DTO mapping. No AutoMapper: for this size it's clearer and faster.</summary>
public static class Mappers
{
    public static UserDto ToDto(this User u) => new(u.Id, u.Name, u.Email, u.Role);

    public static ConsultantDto ToDto(this Consultant c) =>
        new(c.Id, c.Name, c.Email, c.Seniority, c.Location, c.Availability, c.HourlyRate, c.Skills);

    public static void Apply(this ConsultantRequest r, Consultant c)
    {
        c.Name = r.Name; c.Email = r.Email; c.Seniority = r.Seniority; c.Location = r.Location;
        c.Availability = r.Availability; c.HourlyRate = r.HourlyRate;
        c.Skills = r.Skills ?? new();
    }

    public static ClientDto ToDto(this Client c) =>
        new(c.Id, c.Name, c.Industry, c.ContactName,
            c.Demands?.Count(d => d.Status == DemandStatus.Open) ?? 0);

    public static DemandDto ToDto(this Demand d) =>
        new(d.Id, d.ClientId, d.Client?.Name ?? string.Empty, d.Title, d.Description,
            d.RequiredSeniority, d.RequiredSkills, d.Status);

    public static AllocationDto ToDto(this Allocation a) =>
        new(a.Id, a.DemandId, a.Demand?.Title ?? string.Empty, a.ConsultantId,
            a.Consultant?.Name ?? string.Empty, a.StartDate, a.EndDate, a.Status);
}
