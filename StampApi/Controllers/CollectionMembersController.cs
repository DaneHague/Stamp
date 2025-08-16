using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StampApi.Data;
using StampApi.Models;
using System.Security.Claims;

namespace StampApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CollectionMembersController : ControllerBase
{
    private readonly StampDbContext _context;

    public CollectionMembersController(StampDbContext context)
    {
        _context = context;
    }

    [HttpGet("collection/{collectionId}")]
    public async Task<ActionResult<List<CollectionMember>>> GetCollectionMembers(int collectionId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        // Check if user is a member of this collection
        var userMembership = await _context.CollectionMembers
            .FirstOrDefaultAsync(cm => cm.CollectionId == collectionId && cm.UserId == userId);

        if (userMembership == null)
            return Forbid("You are not a member of this collection");

        var members = await _context.CollectionMembers
            .Include(cm => cm.User)
            .Where(cm => cm.CollectionId == collectionId)
            .OrderBy(cm => cm.Role)
            .ThenBy(cm => cm.JoinedAt)
            .ToListAsync();

        return members;
    }

    [HttpPut("{id}/role")]
    public async Task<ActionResult> UpdateMemberRole(int id, [FromBody] UpdateRoleRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var member = await _context.CollectionMembers
            .FirstOrDefaultAsync(cm => cm.Id == id);

        if (member == null)
            return NotFound();

        // Check if current user has permission to update roles
        var currentUserMembership = await _context.CollectionMembers
            .FirstOrDefaultAsync(cm => cm.CollectionId == member.CollectionId && cm.UserId == userId);

        if (currentUserMembership == null || currentUserMembership.Role != CollectionRole.Owner)
            return Forbid("Only collection owners can update member roles");

        // Cannot change your own role
        if (member.UserId == userId)
            return BadRequest("You cannot change your own role");

        // Cannot remove the last owner
        if (member.Role == CollectionRole.Owner && request.Role != CollectionRole.Owner)
        {
            var ownerCount = await _context.CollectionMembers
                .CountAsync(cm => cm.CollectionId == member.CollectionId && cm.Role == CollectionRole.Owner);

            if (ownerCount <= 1)
                return BadRequest("Cannot remove the last owner from the collection");
        }

        member.Role = request.Role;
        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> RemoveMember(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var member = await _context.CollectionMembers
            .FirstOrDefaultAsync(cm => cm.Id == id);

        if (member == null)
            return NotFound();

        // Check permissions
        var currentUserMembership = await _context.CollectionMembers
            .FirstOrDefaultAsync(cm => cm.CollectionId == member.CollectionId && cm.UserId == userId);

        if (currentUserMembership == null)
            return Forbid("You are not a member of this collection");

        // Users can remove themselves, or owners/admins can remove others
        if (member.UserId != userId && 
            (currentUserMembership.Role != CollectionRole.Owner && currentUserMembership.Role != CollectionRole.Admin))
            return Forbid("You don't have permission to remove this member");

        // Cannot remove the last owner
        if (member.Role == CollectionRole.Owner)
        {
            var ownerCount = await _context.CollectionMembers
                .CountAsync(cm => cm.CollectionId == member.CollectionId && cm.Role == CollectionRole.Owner);

            if (ownerCount <= 1)
                return BadRequest("Cannot remove the last owner from the collection");
        }

        _context.CollectionMembers.Remove(member);
        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("leave/{collectionId}")]
    public async Task<ActionResult> LeaveCollection(int collectionId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var member = await _context.CollectionMembers
            .FirstOrDefaultAsync(cm => cm.CollectionId == collectionId && cm.UserId == userId);

        if (member == null)
            return NotFound("You are not a member of this collection");

        // Cannot leave if you're the last owner
        if (member.Role == CollectionRole.Owner)
        {
            var ownerCount = await _context.CollectionMembers
                .CountAsync(cm => cm.CollectionId == collectionId && cm.Role == CollectionRole.Owner);

            if (ownerCount <= 1)
                return BadRequest("Cannot leave collection as the last owner. Transfer ownership or delete the collection.");
        }

        _context.CollectionMembers.Remove(member);
        await _context.SaveChangesAsync();

        return Ok();
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}

public class UpdateRoleRequest
{
    public CollectionRole Role { get; set; }
}