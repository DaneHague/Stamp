using System.Net.Http.Json;
using StampBlazor.Models;

namespace StampBlazor.Services;

public class CollectionService
{
    private readonly AuthenticatedHttpClient _authenticatedHttpClient;

    public CollectionService(AuthenticatedHttpClient authenticatedHttpClient)
    {
        _authenticatedHttpClient = authenticatedHttpClient;
    }

    public async Task<List<Collection>> GetCollectionsAsync()
    {
        try
        {
            var httpClient = await _authenticatedHttpClient.GetHttpClientAsync();
            Console.WriteLine("Frontend: Fetching collections from api/collections");
            var collections = await httpClient.GetFromJsonAsync<List<Collection>>("api/collections");
            Console.WriteLine($"Frontend: Received {collections?.Count ?? 0} collections");
            return collections ?? new List<Collection>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Frontend: Error fetching collections: {ex.Message}");
            return new List<Collection>();
        }
    }

    public async Task<List<Collection>> GetCollectionsByWorkspaceAsync(int workspaceId)
    {
        try
        {
            var httpClient = await _authenticatedHttpClient.GetHttpClientAsync();
            Console.WriteLine($"Frontend: Fetching collections for workspace {workspaceId}");
            var collections = await httpClient.GetFromJsonAsync<List<Collection>>($"api/collections?workspaceId={workspaceId}");
            Console.WriteLine($"Frontend: Received {collections?.Count ?? 0} collections for workspace {workspaceId}");
            return collections ?? new List<Collection>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Frontend: Error fetching collections for workspace {workspaceId}: {ex.Message}");
            return new List<Collection>();
        }
    }

    public async Task<Collection?> GetCollectionAsync(int id)
    {
        var httpClient = await _authenticatedHttpClient.GetHttpClientAsync();
        return await httpClient.GetFromJsonAsync<Collection>($"api/collections/{id}");
    }

    public async Task<Collection?> CreateCollectionAsync(Collection collection)
    {
        try
        {
            var httpClient = await _authenticatedHttpClient.GetHttpClientAsync();
            var response = await httpClient.PostAsJsonAsync("api/collections", collection);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Collection>();
            }
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Frontend: Error creating collection: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> UpdateCollectionAsync(Collection collection)
    {
        var httpClient = await _authenticatedHttpClient.GetHttpClientAsync();
        var response = await httpClient.PutAsJsonAsync($"api/collections/{collection.Id}", collection);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteCollectionAsync(int id)
    {
        var httpClient = await _authenticatedHttpClient.GetHttpClientAsync();
        var response = await httpClient.DeleteAsync($"api/collections/{id}");
        return response.IsSuccessStatusCode;
    }
}