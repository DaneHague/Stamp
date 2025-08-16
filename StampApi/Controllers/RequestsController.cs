using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using StampApi.Data;
using StampApi.Models;

namespace StampApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RequestsController : ControllerBase
{
    private readonly StampDbContext _context;

    public RequestsController(StampDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ApiRequest>>> GetApiRequests()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        
        return await _context.ApiRequests
            .Include(r => r.Collection)
            .Where(r => r.Collection!.UserId == userId)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiRequest>> GetApiRequest(int id)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        
        var apiRequest = await _context.ApiRequests
            .Include(r => r.Collection)
            .FirstOrDefaultAsync(r => r.Id == id && r.Collection!.UserId == userId);

        if (apiRequest == null)
        {
            return NotFound();
        }

        return apiRequest;
    }

    [HttpGet("collection/{collectionId}")]
    public async Task<ActionResult<IEnumerable<ApiRequest>>> GetRequestsByCollection(int collectionId)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        
        // Verify the collection belongs to the user
        var collection = await _context.Collections.FindAsync(collectionId);
        if (collection == null || collection.UserId != userId)
        {
            return NotFound();
        }
        
        return await _context.ApiRequests
            .Where(r => r.CollectionId == collectionId)
            .Include(r => r.Collection)
            .ToListAsync();
    }

    [HttpPost]
    public async Task<ActionResult<ApiRequest>> PostApiRequest(ApiRequest apiRequest)
    {
        
        // Clear navigation properties to avoid EF tracking issues
        apiRequest.Collection = null;
        
        // Set timestamps
        apiRequest.CreatedAt = DateTime.UtcNow;
        apiRequest.UpdatedAt = DateTime.UtcNow;
        
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        
        // Validate that the collection exists and belongs to the user
        var collection = await _context.Collections.FindAsync(apiRequest.CollectionId);
        if (collection == null || collection.UserId != userId)
        {
            return BadRequest("Collection does not exist");
        }
        
        _context.ApiRequests.Add(apiRequest);
        await _context.SaveChangesAsync();
        
        return CreatedAtAction(nameof(GetApiRequest), new { id = apiRequest.Id }, apiRequest);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutApiRequest(int id, ApiRequest apiRequest)
    {
        if (id != apiRequest.Id)
        {
            return BadRequest();
        }

        // Clear navigation properties to avoid EF tracking issues
        apiRequest.Collection = null;
        
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        
        // Validate that the collection exists and belongs to the user
        var collection = await _context.Collections.FindAsync(apiRequest.CollectionId);
        if (collection == null || collection.UserId != userId)
        {
            return BadRequest("Collection does not exist");
        }

        apiRequest.UpdatedAt = DateTime.UtcNow;
        _context.Entry(apiRequest).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ApiRequestExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteApiRequest(int id)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        
        var apiRequest = await _context.ApiRequests
            .Include(r => r.Collection)
            .FirstOrDefaultAsync(r => r.Id == id && r.Collection!.UserId == userId);
            
        if (apiRequest == null)
        {
            return NotFound();
        }

        _context.ApiRequests.Remove(apiRequest);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool ApiRequestExists(int id)
    {
        return _context.ApiRequests.Any(e => e.Id == id);
    }
    
    private int? GetUserId()
    {
        if (User?.Identity?.IsAuthenticated != true)
            return null;
            
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}