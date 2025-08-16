using System.Text.Json.Serialization;

namespace StampApi.Models;

public class CollectionMember
{
    public int Id { get; set; }
    public int CollectionId { get; set; }
    public int UserId { get; set; }
    public CollectionRole Role { get; set; } = CollectionRole.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    
    [JsonIgnore]
    public Collection Collection { get; set; } = null!;
    
    [JsonIgnore]
    public ApplicationUser User { get; set; } = null!;
}

public enum CollectionRole
{
    Owner = 1,
    Admin = 2,
    Member = 3
}