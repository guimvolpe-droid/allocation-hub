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
[Route("api/allocations")]
public class AllocationsController : ControllerBase
{
    private readonly AppDbContext _db;
    public AllocationsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<AllocationDto>>> List() =>
        await _db.Allocations.AsNoTracking().Include(a => a.Demand).Include(a => a.Consultant)
            .OrderByDescending(a => a.Status == AllocationStatus.Active).ThenByDescending(a => a.StartDate)
            .Select(a => a.ToDto()).ToListAsync();

    /// <summary>Allocate a consultant to a demand: marks both as busy and closes the demand.</summary>
    [HttpPost]
    public async Task<ActionResult<AllocationDto>> Create(AllocationRequest req)
    {
        var demand = await _db.Demands.FindAsync(req.DemandId);
        var consultant = await _db.Consultants.FindAsync(req.ConsultantId);
        if (demand is null || consultant is null)
            return BadRequest(new { message = "Demand or consultant not found." });
        if (consultant.Availability == Availability.Allocated)
            return Conflict(new { message = "Consultant is already allocated." });

        var alloc = new Allocation
        {
            DemandId = demand.Id, ConsultantId = consultant.Id,
            // Honor the requested start date (the contract sends it); default to today if unset.
            StartDate = req.StartDate == default ? DateOnly.FromDateTime(DateTime.UtcNow.Date) : req.StartDate,
            Status = AllocationStatus.Active
        };
        _db.Allocations.Add(alloc);
        consultant.Availability = Availability.Allocated;
        demand.Status = DemandStatus.Allocated;
        await _db.SaveChangesAsync();

        await _db.Entry(alloc).Reference(a => a.Demand).LoadAsync();
        await _db.Entry(alloc).Reference(a => a.Consultant).LoadAsync();
        return CreatedAtAction(nameof(List), alloc.ToDto());
    }

    /// <summary>End an allocation: frees the consultant and re-opens the demand.</summary>
    [HttpPost("{id:int}/end")]
    public async Task<ActionResult<AllocationDto>> End(int id)
    {
        var alloc = await _db.Allocations.Include(a => a.Demand).Include(a => a.Consultant)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (alloc is null) return NotFound();
        if (alloc.Status == AllocationStatus.Ended) return Ok(alloc.ToDto());

        alloc.Status = AllocationStatus.Ended;
        alloc.EndDate = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        if (alloc.Consultant is not null) alloc.Consultant.Availability = Availability.Available;
        if (alloc.Demand is not null) alloc.Demand.Status = DemandStatus.Open;
        await _db.SaveChangesAsync();
        return alloc.ToDto();
    }
}
