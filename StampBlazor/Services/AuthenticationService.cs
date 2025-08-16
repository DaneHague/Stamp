using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;

namespace StampBlazor.Services;

public class AuthenticationService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private string? _jwtToken;
    private User? _currentUser;

    public event Action? AuthenticationStateChanged;

    public AuthenticationService(HttpClient httpClient, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
    }

    public bool IsAuthenticated => !string.IsNullOrEmpty(_jwtToken) && _currentUser != null;
    public User? CurrentUser => _currentUser;
    public string? Token => _jwtToken;

    public async Task<bool> InitializeGoogleAuth()
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
                    _currentUser = JsonSerializer.Deserialize<User>(userJson);
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

    public async Task<bool> SignInWithGoogle()
    {
        try
        {
            // Initialize Google Sign-In
            await _jsRuntime.InvokeVoidAsync("initializeGoogleSignIn");
            
            // Get Google user data
            var googleUser = await _jsRuntime.InvokeAsync<GoogleUser>("signInWithGoogle");
            
            if (googleUser != null && !string.IsNullOrEmpty(googleUser.Id))
            {
                // Send to our API for authentication
                var authRequest = new GoogleAuthRequest
                {
                    GoogleId = googleUser.Id,
                    Email = googleUser.Email,
                    Name = googleUser.Name,
                    AvatarUrl = googleUser.Picture
                };
                
                var response = await _httpClient.PostAsJsonAsync("api/auth/google", authRequest);
                
                if (response.IsSuccessStatusCode)
                {
                    var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
                    if (authResponse != null)
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
                        return true;
                    }
                }
            }
            
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Sign-in error: {ex.Message}");
            return false;
        }
    }

    public async Task SignOut()
    {
        try
        {
            // Sign out from Google
            await _jsRuntime.InvokeVoidAsync("signOutFromGoogle");
            
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
}

public class GoogleUser
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Picture { get; set; } = string.Empty;
}

public class GoogleAuthRequest
{
    public string GoogleId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public User User { get; set; } = new();
}

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}