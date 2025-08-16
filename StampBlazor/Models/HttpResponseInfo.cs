using System.Net;

namespace StampBlazor.Models;

public class HttpResponseInfo
{
    public HttpStatusCode StatusCode { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new();
    public string Body { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
}