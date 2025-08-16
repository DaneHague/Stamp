using StampBlazor.Models;
using System.Text.Json;

namespace StampBlazor.Services;

public class CollectionInviteService
{
    private readonly AuthenticatedHttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public CollectionInviteService(AuthenticatedHttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<CollectionInvite?> CreateInviteAsync(int collectionId, string email, CollectionRole role = CollectionRole.Member)
    {
        try
        {
            var request = new
            {
                CollectionId = collectionId,
                Email = email,
                Role = role
            };

            var response = await _httpClient.PostAsJsonAsync("api/collectioninvites", request, _jsonOptions);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<CollectionInvite>(jsonResponse, _jsonOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating invite: {ex.Message}");
            return null;
        }
    }

    public async Task<CollectionInvite?> GetInviteByTokenAsync(string token)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/collectioninvites/token/{token}");
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<CollectionInvite>(jsonResponse, _jsonOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting invite: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> AcceptInviteAsync(int inviteId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/collectioninvites/{inviteId}/accept", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error accepting invite: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeclineInviteAsync(int inviteId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/collectioninvites/{inviteId}/decline", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error declining invite: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> CancelInviteAsync(int inviteId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/collectioninvites/{inviteId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error canceling invite: {ex.Message}");
            return false;
        }
    }

    public async Task<List<CollectionInvite>> GetCollectionInvitesAsync(int collectionId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/collectioninvites/collection/{collectionId}");
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<CollectionInvite>>(jsonResponse, _jsonOptions) ?? new List<CollectionInvite>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting collection invites: {ex.Message}");
            return new List<CollectionInvite>();
        }
    }
}