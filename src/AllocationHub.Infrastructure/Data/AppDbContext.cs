using AllocationHub.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace AllocationHub.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Consultant> Consultants => Set<Consultant>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Demand> Demands => Set<Demand>();
    public DbSet<Allocation> Allocations => Set<Allocation>();
    public DbSet<MatchingSettings> MatchingSettings => Set<MatchingSettings>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Skills/RequiredSkills are List<string>. SQLite has no array type, so we store them as a
        // single delimited string via a value converter + comparer. Fine for the MVP; a normalized
        // Skill table would be the next step if we needed querying/analytics on skills.
        var splitter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<List<string>, string>(
            v => string.Join('|', v),
            v => v.Length == 0 ? new List<string>() : v.Split('|', StringSplitOptions.RemoveEmptyEntries).ToList());

        var comparer = new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<string>>(
            (a, c) => (a ?? new()).SequenceEqual(c ?? new()),
            v => v.Aggregate(0, (h, s) => HashCode.Combine(h, s.GetHashCode())),
            v => v.ToList());

        b.Entity<Consultant>().Property(c => c.Skills).HasConversion(splitter, comparer);
        b.Entity<Consultant>().Property(c => c.HourlyRate).HasColumnType("decimal(10,2)");
        b.Entity<Demand>().Property(d => d.RequiredSkills).HasConversion(splitter, comparer);

        b.Entity<User>().HasIndex(u => u.Email).IsUnique();

        b.Entity<Demand>()
            .HasOne(d => d.Client).WithMany(c => c.Demands)
            .HasForeignKey(d => d.ClientId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<Allocation>().HasOne(a => a.Demand).WithMany().HasForeignKey(a => a.DemandId);
        b.Entity<Allocation>().HasOne(a => a.Consultant).WithMany().HasForeignKey(a => a.ConsultantId);
    }
}
