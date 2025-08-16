using System.Text.Json.Serialization;

namespace StampApi.Models;

public class User
{
    public int Id { get; set; }
    public string GoogleId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;
    
    [JsonIgnore]
    public ICollection<Collection> Collections { get; set; } = new List<Collection>();
}