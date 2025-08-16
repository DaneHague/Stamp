using System.Text.Json.Serialization;

namespace StampApi.Models;

public class Workspace
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public int UserId { get; set; }
    
    [JsonIgnore]
    public ApplicationUser? User { get; set; }
    
    public ICollection<Collection> Collections { get; set; } = new List<Collection>();
}