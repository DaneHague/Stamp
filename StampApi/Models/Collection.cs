using System.Text.Json.Serialization;

namespace StampApi.Models;

public class Collection
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public int? UserId { get; set; }
    public int? WorkspaceId { get; set; }
    
    [JsonIgnore]
    public ApplicationUser? User { get; set; }
    
    [JsonIgnore]
    public Workspace? Workspace { get; set; }
    
    public ICollection<ApiRequest>? Requests { get; set; } = new List<ApiRequest>();
    
    [JsonIgnore]
    public ICollection<CollectionMember> Members { get; set; } = new List<CollectionMember>();
    
    [JsonIgnore]
    public ICollection<CollectionInvite> Invites { get; set; } = new List<CollectionInvite>();
}