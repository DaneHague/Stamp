using System.Text.Json.Serialization;

namespace StampApi.Models;

public class CollectionInvite
{
    public int Id { get; set; }
    public int CollectionId { get; set; }
    public int InvitedByUserId { get; set; }
    public string InvitedEmail { get; set; } = string.Empty;
    public CollectionRole Role { get; set; } = CollectionRole.Member;
    public string InviteToken { get; set; } = string.Empty;
    public InviteStatus Status { get; set; } = InviteStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(7);
    public DateTime? AcceptedAt { get; set; }
    public int? AcceptedByUserId { get; set; }
    
    [JsonIgnore]
    public Collection Collection { get; set; } = null!;
    
    [JsonIgnore]
    public ApplicationUser InvitedByUser { get; set; } = null!;
    
    [JsonIgnore]
    public ApplicationUser? AcceptedByUser { get; set; }
}

public enum InviteStatus
{
    Pending = 1,
    Accepted = 2,
    Declined = 3,
    Expired = 4,
    Cancelled = 5
}