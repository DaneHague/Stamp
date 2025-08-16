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
public class CollectionInvitesController : ControllerBase
{
    private readonly StampDbContext _context;

    public CollectionInvitesController(StampDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<ActionResult<CollectionInvite>> CreateInvite([FromBody] CreateInviteRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        // Check if user has permission to invite to this collection
        var userMembership = await _context.CollectionMembers
            .FirstOrDefaultAsync(cm => cm.CollectionId == request.CollectionId && cm.UserId == userId);

        if (userMembership == null || (userMembership.Role != CollectionRole.Owner && userMembership.Role != CollectionRole.Admin))
            return Forbid("You don't have permission to invite users to this collection");

        // Check if user is already a member
        var existingMember = await _context.CollectionMembers
            .Include(cm => cm.User)
            .FirstOrDefaultAsync(cm => cm.CollectionId == request.CollectionId && cm.User.Email == request.Email);

        if (existingMember != null)
            return BadRequest("User is already a member of this collection");

        // Check if there's already a pending invite
        var existingInvite = await _context.CollectionInvites
            .FirstOrDefaultAsync(ci => ci.CollectionId == request.CollectionId && 
                                      ci.InvitedEmail == request.Email && 
                                      ci.Status == InviteStatus.Pending);

        if (existingInvite != null)
            return BadRequest("There's already a pending invite for this email");

        var invite = new CollectionInvite
        {
            CollectionId = request.CollectionId,
            InvitedByUserId = userId.Value,
            InvitedEmail = request.Email,
            Role = request.Role,
            InviteToken = Guid.NewGuid().ToString("N"),
            Status = InviteStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _context.CollectionInvites.Add(invite);
        await _context.SaveChangesAsync();

        // Load related data for response
        await _context.Entry(invite)
            .Reference(i => i.InvitedByUser)
            .LoadAsync();

        return CreatedAtAction(nameof(GetInvite), new { id = invite.Id }, invite);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CollectionInvite>> GetInvite(int id)
    {
        var invite = await _context.CollectionInvites
            .Include(i => i.Collection)
            .Include(i => i.InvitedByUser)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invite == null)
            return NotFound();

        return invite;
    }

    [HttpGet("token/{token}")]
    public async Task<ActionResult<CollectionInvite>> GetInviteByToken(string token)
    {
        var invite = await _context.CollectionInvites
            .Include(i => i.Collection)
            .Include(i => i.InvitedByUser)
            .FirstOrDefaultAsync(i => i.InviteToken == token);

        if (invite == null)
            return NotFound();

        if (invite.Status != InviteStatus.Pending || invite.ExpiresAt < DateTime.UtcNow)
            return BadRequest("Invite is expired or no longer valid");

        return invite;
    }

    [HttpPost("{id}/accept")]
    public async Task<ActionResult> AcceptInvite(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var invite = await _context.CollectionInvites
            .Include(i => i.Collection)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invite == null)
            return NotFound();

        if (invite.Status != InviteStatus.Pending)
            return BadRequest("Invite is no longer pending");

        if (invite.ExpiresAt < DateTime.UtcNow)
            return BadRequest("Invite has expired");

        // Get current user's email to verify they can accept this invite
        var currentUser = await _context.Users.FindAsync(userId.Value);
        if (currentUser == null || currentUser.Email != invite.InvitedEmail)
            return Forbid("You cannot accept this invite");

        // Check if user is already a member
        var existingMember = await _context.CollectionMembers
            .FirstOrDefaultAsync(cm => cm.CollectionId == invite.CollectionId && cm.UserId == userId);

        if (existingMember != null)
            return BadRequest("You are already a member of this collection");

        // Create membership
        var member = new CollectionMember
        {
            CollectionId = invite.CollectionId,
            UserId = userId.Value,
            Role = invite.Role,
            JoinedAt = DateTime.UtcNow
        };

        _context.CollectionMembers.Add(member);

        // Update invite status
        invite.Status = InviteStatus.Accepted;
        invite.AcceptedAt = DateTime.UtcNow;
        invite.AcceptedByUserId = userId.Value;

        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("{id}/decline")]
    public async Task<ActionResult> DeclineInvite(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var invite = await _context.CollectionInvites
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invite == null)
            return NotFound();

        if (invite.Status != InviteStatus.Pending)
            return BadRequest("Invite is no longer pending");

        // Get current user's email to verify they can decline this invite
        var currentUser = await _context.Users.FindAsync(userId.Value);
        if (currentUser == null || currentUser.Email != invite.InvitedEmail)
            return Forbid("You cannot decline this invite");

        invite.Status = InviteStatus.Declined;
        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> CancelInvite(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var invite = await _context.CollectionInvites
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invite == null)
            return NotFound();

        if (invite.InvitedByUserId != userId)
        {
            // Check if user has admin/owner permission
            var userMembership = await _context.CollectionMembers
                .FirstOrDefaultAsync(cm => cm.CollectionId == invite.CollectionId && cm.UserId == userId);

            if (userMembership == null || (userMembership.Role != CollectionRole.Owner && userMembership.Role != CollectionRole.Admin))
                return Forbid("You don't have permission to cancel this invite");
        }

        invite.Status = InviteStatus.Cancelled;
        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpGet("collection/{collectionId}")]
    public async Task<ActionResult<List<CollectionInvite>>> GetCollectionInvites(int collectionId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        // Check if user has permission to view invites for this collection
        var userMembership = await _context.CollectionMembers
            .FirstOrDefaultAsync(cm => cm.CollectionId == collectionId && cm.UserId == userId);

        if (userMembership == null || (userMembership.Role != CollectionRole.Owner && userMembership.Role != CollectionRole.Admin))
            return Forbid("You don't have permission to view invites for this collection");

        var invites = await _context.CollectionInvites
            .Include(i => i.InvitedByUser)
            .Where(i => i.CollectionId == collectionId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        return invites;
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}

public class CreateInviteRequest
{
    public int CollectionId { get; set; }
    public string Email { get; set; } = string.Empty;
    public CollectionRole Role { get; set; } = CollectionRole.Member;
}