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
public class WorkspacesController : ControllerBase
{
    private readonly StampDbContext _context;

    public WorkspacesController(StampDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Workspace>>> GetWorkspaces()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        
        var workspaces = await _context.Workspaces
            .Where(w => w.UserId == userId)
            .Include(w => w.Collections)
                .ThenInclude(c => c.Requests)
            .OrderBy(w => w.CreatedAt)
            .ToListAsync();
        
        return workspaces;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Workspace>> GetWorkspace(int id)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        
        var workspace = await _context.Workspaces
            .Include(w => w.Collections)
                .ThenInclude(c => c.Requests)
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

        if (workspace == null)
        {
            return NotFound();
        }

        return workspace;
    }

    [HttpPost]
    public async Task<ActionResult<Workspace>> PostWorkspace(Workspace workspace)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        
        
        workspace.UserId = userId.Value;
        workspace.CreatedAt = DateTime.UtcNow;
        workspace.UpdatedAt = DateTime.UtcNow;
        
        _context.Workspaces.Add(workspace);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetWorkspace), new { id = workspace.Id }, workspace);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutWorkspace(int id, Workspace workspace)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        
        if (id != workspace.Id)
        {
            return BadRequest();
        }
        
        var existingWorkspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);
            
        if (existingWorkspace == null)
        {
            return NotFound();
        }

        existingWorkspace.Name = workspace.Name;
        existingWorkspace.Description = workspace.Description;
        existingWorkspace.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!WorkspaceExists(id, userId.Value))
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
    public async Task<IActionResult> DeleteWorkspace(int id)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        
        var workspace = await _context.Workspaces
            .Include(w => w.Collections)
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);
            
        if (workspace == null)
        {
            return NotFound();
        }

        // Check if this is the user's only workspace
        var userWorkspaceCount = await _context.Workspaces
            .CountAsync(w => w.UserId == userId);
            
        if (userWorkspaceCount <= 1)
        {
            return BadRequest("Cannot delete your only workspace. Create another workspace first.");
        }

        // Move collections to the user's first remaining workspace
        if (workspace.Collections.Any())
        {
            var targetWorkspace = await _context.Workspaces
                .FirstOrDefaultAsync(w => w.UserId == userId && w.Id != id);
                
            if (targetWorkspace != null)
            {
                foreach (var collection in workspace.Collections)
                {
                    collection.WorkspaceId = targetWorkspace.Id;
                }
            }
        }

        _context.Workspaces.Remove(workspace);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool WorkspaceExists(int id, int userId)
    {
        return _context.Workspaces.Any(e => e.Id == id && e.UserId == userId);
    }
    
    private int? GetUserId()
    {
        if (User?.Identity?.IsAuthenticated != true)
            return null;
            
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}