using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;

namespace StampBlazor.Services;

public class IdentityAuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private string? _jwtToken;
    private UserDto? _currentUser;

    public event Action? AuthenticationStateChanged;

    public IdentityAuthenticationService(HttpClient httpClient, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
    }

    public bool IsAuthenticated => !string.IsNullOrEmpty(_jwtToken) && _currentUser != null;
    public UserDto? CurrentUser => _currentUser;
    public string? Token => _jwtToken;

    public async Task<bool> InitializeAuth()
    {
        try
        {
            // Load stored token if available
            _jwtToken = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "authToken");
            
            if (!string.IsNullOrEmpty(_jwtToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _jwtToken);
                
                // Try to get user info to validate token
                var userJson = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "currentUser");
                if (!string.IsNullOrEmpty(userJson))
                {
                    _currentUser = JsonSerializer.Deserialize<UserDto>(userJson);
                    AuthenticationStateChanged?.Invoke();
                    return true;
                }
            }
            
            return false;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IdentityAuthResult> RegisterAsync(string email, string password)
    {
        try
        {
            var request = new RegisterRequest
            {
                Email = email,
                Password = password
            };
            
            var response = await _httpClient.PostAsJsonAsync("api/identityauth/register", request);
            
            if (response.IsSuccessStatusCode)
            {
                var authResponse = await response.Content.ReadFromJsonAsync<IdentityAuthResponse>();
                if (authResponse != null)
                {
                    await SetAuthenticationState(authResponse);
                    return new IdentityAuthResult { Success = true };
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new IdentityAuthResult { Success = false, ErrorMessage = errorContent };
            }
            
            return new IdentityAuthResult { Success = false, ErrorMessage = "Registration failed" };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Registration error: {ex.Message}");
            return new IdentityAuthResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task<IdentityAuthResult> LoginAsync(string email, string password)
    {
        try
        {
            var request = new LoginRequest
            {
                Email = email,
                Password = password
            };
            
            var response = await _httpClient.PostAsJsonAsync("api/identityauth/login", request);
            
            if (response.IsSuccessStatusCode)
            {
                var authResponse = await response.Content.ReadFromJsonAsync<IdentityAuthResponse>();
                if (authResponse != null)
                {
                    await SetAuthenticationState(authResponse);
                    return new IdentityAuthResult { Success = true };
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return new IdentityAuthResult { Success = false, ErrorMessage = errorContent };
            }
            
            return new IdentityAuthResult { Success = false, ErrorMessage = "Login failed" };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Login error: {ex.Message}");
            return new IdentityAuthResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    public async Task SignOutAsync()
    {
        try
        {
            // Clear local storage
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "currentUser");
            
            // Clear local state
            _jwtToken = null;
            _currentUser = null;
            _httpClient.DefaultRequestHeaders.Authorization = null;
            
            AuthenticationStateChanged?.Invoke();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Sign-out error: {ex.Message}");
        }
    }

    private async Task SetAuthenticationState(IdentityAuthResponse authResponse)
    {
        _jwtToken = authResponse.Token;
        _currentUser = authResponse.User;
        
        // Store in localStorage
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authToken", _jwtToken);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "currentUser", 
            JsonSerializer.Serialize(_currentUser));
        
        // Set authorization header
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _jwtToken);
        
        AuthenticationStateChanged?.Invoke();
    }
}

public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class IdentityAuthResponse
{
    public string Token { get; set; } = string.Empty;
    public UserDto User { get; set; } = new();
}

public class UserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}

public class IdentityAuthResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}