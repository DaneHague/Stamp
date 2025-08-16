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
public class CollectionsController : ControllerBase
{
    private readonly StampDbContext _context;

    public CollectionsController(StampDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Collection>>> GetCollections([FromQuery] int? workspaceId = null)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        
        // Get collections where user is either the original owner or a member
        // Also ensure the workspace belongs to the user
        IQueryable<Collection> query = _context.Collections
            .Where(c => (c.UserId == userId || c.Members.Any(m => m.UserId == userId)) 
                       && c.Workspace.UserId == userId);

        // Filter by workspace if specified
        if (workspaceId.HasValue)
        {
            query = query.Where(c => c.WorkspaceId == workspaceId.Value);
        }

        var collections = await query
            .Include(c => c.Requests)
            .Include(c => c.Members)
                .ThenInclude(m => m.User)
            .Include(c => c.Workspace)
            .ToListAsync();
        
        return collections;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Collection>> GetCollection(int id)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        
        var collection = await _context.Collections
            .Include(c => c.Requests)
            .Include(c => c.Members)
                .ThenInclude(m => m.User)
            .Include(c => c.Workspace)
            .FirstOrDefaultAsync(c => c.Id == id && (c.UserId == userId || c.Members.Any(m => m.UserId == userId))
                                && c.Workspace.UserId == userId);

        if (collection == null)
        {
            return NotFound();
        }

        return collection;
    }

    [HttpPost]
    public async Task<ActionResult<Collection>> PostCollection(Collection collection)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        
        // Validate that the workspace exists and belongs to the user
        if (collection.WorkspaceId <= 0)
        {
            return BadRequest("WorkspaceId is required");
        }
        
        var workspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == collection.WorkspaceId && w.UserId == userId);
            
        if (workspace == null)
        {
            return BadRequest("Invalid workspace or workspace does not belong to user");
        }
        
        
        collection.UserId = userId.Value;
        collection.CreatedAt = DateTime.UtcNow;
        collection.UpdatedAt = DateTime.UtcNow;
        
        _context.Collections.Add(collection);
        await _context.SaveChangesAsync();

        // Create owner membership
        var ownerMembership = new CollectionMember
        {
            CollectionId = collection.Id,
            UserId = userId.Value,
            Role = CollectionRole.Owner,
            JoinedAt = DateTime.UtcNow
        };
        
        _context.CollectionMembers.Add(ownerMembership);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCollection), new { id = collection.Id }, collection);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutCollection(int id, Collection collection)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        
        if (id != collection.Id)
        {
            return BadRequest();
        }
        
        var existingCollection = await _context.Collections
            .Include(c => c.Members)
            .Include(c => c.Workspace)
            .FirstOrDefaultAsync(c => c.Id == id);
            
        if (existingCollection == null)
        {
            return NotFound();
        }
        
        // Check if user has access to the workspace
        if (existingCollection.Workspace.UserId != userId)
        {
            return Forbid("You don't have access to this workspace");
        }
        
        // Check if user has edit permission (owner or admin)
        var userMembership = existingCollection.Members.FirstOrDefault(m => m.UserId == userId);
        if (existingCollection.UserId != userId && 
            (userMembership == null || (userMembership.Role != CollectionRole.Owner && userMembership.Role != CollectionRole.Admin)))
        {
            return Forbid("You don't have permission to edit this collection");
        }

        collection.UserId = userId.Value;
        collection.UpdatedAt = DateTime.UtcNow;
        _context.Entry(collection).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!CollectionExists(id, userId.Value))
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
    public async Task<IActionResult> DeleteCollection(int id)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        
        var collection = await _context.Collections
            .Include(c => c.Members)
            .Include(c => c.Workspace)
            .FirstOrDefaultAsync(c => c.Id == id);
            
        if (collection == null)
        {
            return NotFound();
        }
        
        // Check if user has access to the workspace
        if (collection.Workspace.UserId != userId)
        {
            return Forbid("You don't have access to this workspace");
        }
        
        // Check if user has delete permission (only owners)
        var userMembership = collection.Members.FirstOrDefault(m => m.UserId == userId);
        if (collection.UserId != userId && 
            (userMembership == null || userMembership.Role != CollectionRole.Owner))
        {
            return Forbid("Only collection owners can delete collections");
        }

        _context.Collections.Remove(collection);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool CollectionExists(int id, int userId)
    {
        return _context.Collections.Any(e => e.Id == id && e.UserId == userId);
    }
    
    private int? GetUserId()
    {
        if (User?.Identity?.IsAuthenticated != true)
            return null;
            
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}