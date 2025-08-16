using System.Net.Http.Json;
using StampBlazor.Models;

namespace StampBlazor.Services;

public class RequestService
{
    private readonly AuthenticatedHttpClient _authenticatedHttpClient;

    public RequestService(AuthenticatedHttpClient authenticatedHttpClient)
    {
        _authenticatedHttpClient = authenticatedHttpClient;
    }

    public async Task<List<ApiRequest>> GetRequestsAsync()
    {
        var httpClient = await _authenticatedHttpClient.GetHttpClientAsync();
        var requests = await httpClient.GetFromJsonAsync<List<ApiRequest>>("api/requests");
        return requests ?? new List<ApiRequest>();
    }

    public async Task<ApiRequest?> GetRequestAsync(int id)
    {
        var httpClient = await _authenticatedHttpClient.GetHttpClientAsync();
        return await httpClient.GetFromJsonAsync<ApiRequest>($"api/requests/{id}");
    }

    public async Task<List<ApiRequest>> GetRequestsByCollectionAsync(int collectionId)
    {
        var httpClient = await _authenticatedHttpClient.GetHttpClientAsync();
        var requests = await httpClient.GetFromJsonAsync<List<ApiRequest>>($"api/requests/collection/{collectionId}");
        return requests ?? new List<ApiRequest>();
    }

    public async Task<ApiRequest?> CreateRequestAsync(ApiRequest request)
    {
        try
        {
            Console.WriteLine($"Frontend: Sending POST to api/requests");
            var httpClient = await _authenticatedHttpClient.GetHttpClientAsync();
            var response = await httpClient.PostAsJsonAsync("api/requests", request);
            Console.WriteLine($"Frontend: Received response status: {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiRequest>();
                Console.WriteLine($"Frontend: Successfully created request with ID {result?.Id}");
                return result;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Frontend: Error response: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Frontend: Exception during request creation: {ex.Message}");
        }
        return null;
    }

    public async Task<bool> UpdateRequestAsync(ApiRequest request)
    {
        var httpClient = await _authenticatedHttpClient.GetHttpClientAsync();
        var response = await httpClient.PutAsJsonAsync($"api/requests/{request.Id}", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteRequestAsync(int id)
    {
        var httpClient = await _authenticatedHttpClient.GetHttpClientAsync();
        var response = await httpClient.DeleteAsync($"api/requests/{id}");
        return response.IsSuccessStatusCode;
    }
}