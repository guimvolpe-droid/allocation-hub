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
[Route("api/clients")]
public class ClientsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ClientsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<ClientDto>>> List() =>
        await _db.Clients.AsNoTracking().Include(c => c.Demands)
            .OrderBy(c => c.Name).Select(c => c.ToDto()).ToListAsync();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClientDto>> Get(int id)
    {
        var c = await _db.Clients.Include(x => x.Demands).FirstOrDefaultAsync(x => x.Id == id);
        return c is null ? NotFound() : c.ToDto();
    }

    [HttpPost]
    public async Task<ActionResult<ClientDto>> Create(ClientRequest req)
    {
        var c = new Client { Name = req.Name, Industry = req.Industry, ContactName = req.ContactName };
        _db.Clients.Add(c);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = c.Id }, c.ToDto());
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ClientDto>> Update(int id, ClientRequest req)
    {
        var c = await _db.Clients.FindAsync(id);
        if (c is null) return NotFound();
        c.Name = req.Name; c.Industry = req.Industry; c.ContactName = req.ContactName;
        await _db.SaveChangesAsync();
        return c.ToDto();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await _db.Clients.FindAsync(id);
        if (c is null) return NotFound();
        _db.Clients.Remove(c);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
