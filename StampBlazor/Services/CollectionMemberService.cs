using StampBlazor.Models;
using System.Text.Json;

namespace StampBlazor.Services;

public class CollectionMemberService
{
    private readonly AuthenticatedHttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public CollectionMemberService(AuthenticatedHttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<List<CollectionMember>> GetCollectionMembersAsync(int collectionId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/collectionmembers/collection/{collectionId}");
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<CollectionMember>>(jsonResponse, _jsonOptions) ?? new List<CollectionMember>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting collection members: {ex.Message}");
            return new List<CollectionMember>();
        }
    }

    public async Task<bool> UpdateMemberRoleAsync(int memberId, CollectionRole role)
    {
        try
        {
            var request = new { Role = role };
            var response = await _httpClient.PutAsJsonAsync($"api/collectionmembers/{memberId}/role", request, _jsonOptions);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating member role: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> RemoveMemberAsync(int memberId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/collectionmembers/{memberId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error removing member: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> LeaveCollectionAsync(int collectionId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"api/collectionmembers/leave/{collectionId}", null);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error leaving collection: {ex.Message}");
            return false;
        }
    }
}