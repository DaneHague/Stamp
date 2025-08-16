namespace StampBlazor.Models;

public class RequestTab
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = "New Request";
    public ApiRequest Request { get; set; } = new() { Method = "GET" };
    public bool IsDirty { get; set; } = false;
    public bool IsActive { get; set; } = false;
}