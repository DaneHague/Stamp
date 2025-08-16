using System.Text.Json.Serialization;

namespace StampBlazor.Models;

public class Collection
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int? UserId { get; set; }
    public int? WorkspaceId { get; set; }
    public List<ApiRequest> Requests { get; set; } = new();
    public List<CollectionMember> Members { get; set; } = new();
    public List<CollectionInvite> Invites { get; set; } = new();
}