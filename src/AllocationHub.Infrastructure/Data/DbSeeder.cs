using AllocationHub.Core.Abstractions;
using AllocationHub.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace AllocationHub.Infrastructure.Data;

/// <summary>
/// Creates the database and seeds demo data on startup if empty. The data is intentionally realistic
/// (fintech / healthcare / logistics clients; skills that look like a real bench) so the demo tells a
/// story: opening the ".NET integration" demand surfaces the strongest available senior at the top.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, IPasswordHasher hasher)
    {
        await db.Database.EnsureCreatedAsync();
        if (await db.Users.AnyAsync()) return; // already seeded

        db.Users.Add(new User
        {
            Name = "Admin", Email = "admin@demo.com",
            PasswordHash = hasher.Hash("admin123"), Role = UserRole.Admin
        });

        // The single active matching configuration (weights). Seeded with the documented defaults.
        db.MatchingSettings.Add(new MatchingSettings { UpdatedBy = "system" });

        var consultants = new List<Consultant>
        {
            new() { Name = "Gustavo Vieira", Email = "gustavo@bench.dev", Seniority = Seniority.Senior,
                    Location = "Araraquara, SP", Availability = Availability.Available, HourlyRate = 120,
                    Skills = new() { ".NET", "Angular", "Kafka", "SQL Server", "Azure", "RAG" } },
            new() { Name = "Ana Souza", Email = "ana@bench.dev", Seniority = Seniority.Senior,
                    Location = "São Paulo, SP", Availability = Availability.Available, HourlyRate = 110,
                    Skills = new() { ".NET", "Angular", "SQL Server" } },
            new() { Name = "Bruno Costa", Email = "bruno@bench.dev", Seniority = Seniority.Mid,
                    Location = "Belo Horizonte, MG", Availability = Availability.Allocated, HourlyRate = 85,
                    Skills = new() { "React", "Node.js", "TypeScript" } },
            new() { Name = "Carla Dias", Email = "carla@bench.dev", Seniority = Seniority.Lead,
                    Location = "Florianópolis, SC", Availability = Availability.Allocated, HourlyRate = 150,
                    Skills = new() { ".NET", "Kafka", "Azure", "Kubernetes" } },
            new() { Name = "Diego Ramos", Email = "diego@bench.dev", Seniority = Seniority.Junior,
                    Location = "Porto Alegre, RS", Availability = Availability.Available, HourlyRate = 55,
                    Skills = new() { "React", "TypeScript" } },
            new() { Name = "Elisa Moraes", Email = "elisa@bench.dev", Seniority = Seniority.Senior,
                    Location = "Remote", Availability = Availability.Available, HourlyRate = 115,
                    Skills = new() { "React", "Node.js", "AWS", "GraphQL" } },
            new() { Name = "Felipe Nunes", Email = "felipe@bench.dev", Seniority = Seniority.Mid,
                    Location = "Curitiba, PR", Availability = Availability.Unavailable, HourlyRate = 80,
                    Skills = new() { ".NET", "SQL Server" } },
            new() { Name = "Gabriela Lima", Email = "gabriela@bench.dev", Seniority = Seniority.Senior,
                    Location = "Recife, PE", Availability = Availability.Available, HourlyRate = 125,
                    Skills = new() { "Python", "RAG", "LangChain", "Vector DB" } },
        };
        db.Consultants.AddRange(consultants);

        var nimbus = new Client { Name = "NimbusPay", Industry = "Fintech (payments)", ContactName = "Carla Menezes" };
        var vita = new Client { Name = "VitaCare Health", Industry = "Healthcare", ContactName = "Dr. Paulo Ribeiro" };
        var rota = new Client { Name = "RotaLog", Industry = "Logistics", ContactName = "Fernanda Alves" };
        db.Clients.AddRange(nimbus, vita, rota);

        var d1 = new Demand { Client = nimbus, Title = "Senior .NET integration engineer",
            Description = "Build payment integrations with external providers over Kafka.",
            RequiredSeniority = Seniority.Senior, RequiredSkills = new() { ".NET", "Kafka", "SQL Server" },
            Status = DemandStatus.Open };
        var d2 = new Demand { Client = vita, Title = "React frontend developer",
            Description = "Patient-facing portal screens.",
            RequiredSeniority = Seniority.Mid, RequiredSkills = new() { "React", "TypeScript" },
            Status = DemandStatus.Allocated };
        var d3 = new Demand { Client = rota, Title = "AI recommendation engineer",
            Description = "Route and carrier recommendation with a RAG pipeline.",
            RequiredSeniority = Seniority.Senior, RequiredSkills = new() { "Python", "RAG", "Vector DB" },
            Status = DemandStatus.Open };
        var d4 = new Demand { Client = nimbus, Title = "Cloud platform lead",
            Description = "Own the Azure/Kubernetes platform for the payments team.",
            RequiredSeniority = Seniority.Lead, RequiredSkills = new() { "Azure", "Kubernetes", ".NET" },
            Status = DemandStatus.Allocated };
        db.Demands.AddRange(d1, d2, d3, d4);

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        db.Allocations.AddRange(
            new Allocation { Demand = d2, Consultant = consultants[2], StartDate = today.AddDays(-20), Status = AllocationStatus.Active },
            new Allocation { Demand = d4, Consultant = consultants[3], StartDate = today.AddDays(-45), Status = AllocationStatus.Active });

        await db.SaveChangesAsync();
    }
}
