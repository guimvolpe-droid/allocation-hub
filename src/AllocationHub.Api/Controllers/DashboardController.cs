using AllocationHub.Api.Mapping;
using AllocationHub.Core.Domain;
using AllocationHub.Core.Dtos;
using AllocationHub.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AllocationHub.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;
    public DashboardController(AppDbContext db) => _db = db;

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummary>> Summary()
    {
        var consultants = await _db.Consultants.AsNoTracking().ToListAsync();
        var openDemands = await _db.Demands.AsNoTracking().Include(d => d.Client)
            .Where(d => d.Status == DemandStatus.Open).ToListAsync();

        return new DashboardSummary(
            TotalConsultants: consultants.Count,
            AvailableConsultants: consultants.Count(c => c.Availability == Availability.Available),
            AllocatedConsultants: consultants.Count(c => c.Availability == Availability.Allocated),
            OpenDemands: openDemands.Count,
            TopOpenDemands: openDemands.OrderBy(d => d.Title).Take(5).Select(d => d.ToDto()).ToList(),
            AvailableNow: consultants.Where(c => c.Availability == Availability.Available)
                .OrderByDescending(c => c.Seniority).Take(5).Select(c => c.ToDto()).ToList());
    }
}
