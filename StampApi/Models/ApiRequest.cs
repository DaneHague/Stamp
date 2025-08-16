using System.Text.Json.Serialization;

namespace StampApi.Models;

public class ApiRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Method { get; set; } = "GET";
    public string? Headers { get; set; }
    public string? Body { get; set; }
    public string? QueryParams { get; set; }
    public string? Authentication { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public int CollectionId { get; set; }
    
    [JsonIgnore]
    public Collection? Collection { get; set; }
}