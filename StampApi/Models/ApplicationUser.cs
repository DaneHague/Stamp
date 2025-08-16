using Microsoft.AspNetCore.Identity;
using System.Text.Json.Serialization;

namespace StampApi.Models;

public class ApplicationUser : IdentityUser<int>
{
    public string? GoogleId { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;
    
    [JsonIgnore]
    public ICollection<Collection> Collections { get; set; } = new List<Collection>();
    
    [JsonIgnore]
    public ICollection<Workspace> Workspaces { get; set; } = new List<Workspace>();
    
    // Override to use int as key type
    public override int Id { get; set; }
}