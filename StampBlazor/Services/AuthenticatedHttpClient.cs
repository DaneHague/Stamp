using Microsoft.JSInterop;
using System.Text.Json;
using System.Net.Http.Json;

namespace StampBlazor.Services;

public class AuthenticatedHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;

    public AuthenticatedHttpClient(HttpClient httpClient, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
    }

    public async Task<HttpClient> GetHttpClientAsync()
    {
        // Always check for and set the latest token
        var token = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "authToken");
        
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
        else
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
        
        return _httpClient;
    }

    public async Task<HttpResponseMessage> GetAsync(string requestUri)
    {
        var httpClient = await GetHttpClientAsync();
        return await httpClient.GetAsync(requestUri);
    }

    public async Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent? content)
    {
        var httpClient = await GetHttpClientAsync();
        return await httpClient.PostAsync(requestUri, content);
    }

    public async Task<HttpResponseMessage> PostAsJsonAsync<T>(string requestUri, T value, JsonSerializerOptions? options = null)
    {
        var httpClient = await GetHttpClientAsync();
        return await httpClient.PostAsJsonAsync(requestUri, value, options);
    }

    public async Task<HttpResponseMessage> PutAsync(string requestUri, HttpContent? content)
    {
        var httpClient = await GetHttpClientAsync();
        return await httpClient.PutAsync(requestUri, content);
    }

    public async Task<HttpResponseMessage> PutAsJsonAsync<T>(string requestUri, T value, JsonSerializerOptions? options = null)
    {
        var httpClient = await GetHttpClientAsync();
        return await httpClient.PutAsJsonAsync(requestUri, value, options);
    }

    public async Task<HttpResponseMessage> DeleteAsync(string requestUri)
    {
        var httpClient = await GetHttpClientAsync();
        return await httpClient.DeleteAsync(requestUri);
    }
}