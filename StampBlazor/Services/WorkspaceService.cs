using System.Net.Http.Json;
using StampBlazor.Models;

namespace StampBlazor.Services;

public class WorkspaceService
{
    private readonly AuthenticatedHttpClient _authenticatedHttpClient;

    public WorkspaceService(AuthenticatedHttpClient authenticatedHttpClient)
    {
        _authenticatedHttpClient = authenticatedHttpClient;
    }

    public async Task<List<Workspace>> GetWorkspacesAsync()
    {
        try
        {
            var httpClient = await _authenticatedHttpClient.GetHttpClientAsync();
            Console.WriteLine("Frontend: Fetching workspaces from api/workspaces");
            var workspaces = await httpClient.GetFromJsonAsync<List<Workspace>>("api/workspaces");
            Console.WriteLine($"Frontend: Received {workspaces?.Count ?? 0} workspaces");
            return workspaces ?? new List<Workspace>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Frontend: Error fetching workspaces: {ex.Message}");
            return new List<Workspace>();
        }
    }

    public async Task<Workspace?> GetWorkspaceAsync(int id)
    {
        try
        {
            var httpClient = await _authenticatedHttpClient.GetHttpClientAsync();
            return await httpClient.GetFromJsonAsync<Workspace>($"api/workspaces/{id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Frontend: Error fetching workspace {id}: {ex.Message}");
            return null;
        }
    }

    public async Task<Workspace?> CreateWorkspaceAsync(Workspace workspace)
    {
        try
        {
            var httpClient = await _authenticatedHttpClient.GetHttpClientAsync();
            var response = await httpClient.PostAsJsonAsync("api/workspaces", workspace);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Workspace>();
            }
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Frontend: Error creating workspace: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> UpdateWorkspaceAsync(Workspace workspace)
    {
        try
        {
            var httpClient = await _authenticatedHttpClient.GetHttpClientAsync();
            var response = await httpClient.PutAsJsonAsync($"api/workspaces/{workspace.Id}", workspace);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Frontend: Error updating workspace: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteWorkspaceAsync(int id)
    {
        try
        {
            var httpClient = await _authenticatedHttpClient.GetHttpClientAsync();
            var response = await httpClient.DeleteAsync($"api/workspaces/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Frontend: Error deleting workspace: {ex.Message}");
            return false;
        }
    }
}