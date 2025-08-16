using System.Text.Json.Serialization;

namespace StampBlazor.Models;

public class RequestAuthentication
{
    public AuthenticationType Type { get; set; } = AuthenticationType.None;
    
    // Basic Auth
    public string? Username { get; set; }
    public string? Password { get; set; }
    
    // Bearer Token
    public string? Token { get; set; }
    
    // API Key
    public string? ApiKeyName { get; set; }
    public string? ApiKeyValue { get; set; }
    public ApiKeyLocation ApiKeyLocation { get; set; } = ApiKeyLocation.Header;
}

public enum ApiKeyLocation
{
    Header,
    QueryParameter
}