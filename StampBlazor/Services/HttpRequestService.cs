using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using StampBlazor.Models;

namespace StampBlazor.Services;

public class HttpRequestService
{
    private readonly HttpClient _httpClient;

    public HttpRequestService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<HttpResponseInfo> SendRequestAsync(string url, string method, string? headers, string? body, string? queryParams, string? authentication = null)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var requestUri = BuildUri(url, queryParams);
            var request = new HttpRequestMessage(new HttpMethod(method.ToUpper()), requestUri);

            if (!string.IsNullOrWhiteSpace(headers))
            {
                SetHeaders(request, headers);
            }

            if (!string.IsNullOrWhiteSpace(authentication))
            {
                requestUri = ApplyAuthentication(request, authentication, requestUri);
                request.RequestUri = new Uri(requestUri);
            }

            if (!string.IsNullOrWhiteSpace(body) && (method.ToUpper() == "POST" || method.ToUpper() == "PUT" || method.ToUpper() == "PATCH"))
            {
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");
            }

            var response = await _httpClient.SendAsync(request);
            stopwatch.Stop();

            var responseBody = await response.Content.ReadAsStringAsync();
            var responseHeaders = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value));

            if (response.Content.Headers != null)
            {
                foreach (var header in response.Content.Headers)
                {
                    responseHeaders[header.Key] = string.Join(", ", header.Value);
                }
            }

            return new HttpResponseInfo
            {
                StatusCode = response.StatusCode,
                StatusText = response.ReasonPhrase ?? response.StatusCode.ToString(),
                Headers = responseHeaders,
                Body = responseBody,
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new HttpResponseInfo
            {
                StatusCode = System.Net.HttpStatusCode.BadRequest,
                StatusText = ex.Message,
                Headers = new Dictionary<string, string>(),
                Body = $"Error: {ex.Message}",
                Duration = stopwatch.Elapsed
            };
        }
    }

    private static string BuildUri(string url, string? queryParams)
    {
        if (string.IsNullOrWhiteSpace(queryParams))
            return url;

        var separator = url.Contains('?') ? "&" : "?";
        return $"{url}{separator}{queryParams}";
    }

    private static void SetHeaders(HttpRequestMessage request, string headers)
    {
        try
        {
            var headerDict = JsonSerializer.Deserialize<Dictionary<string, string>>(headers);
            if (headerDict != null)
            {
                foreach (var header in headerDict)
                {
                    if (IsContentHeader(header.Key))
                    {
                        continue;
                    }
                    
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }
        }
        catch
        {
            // Fallback: try to parse as key:value pairs separated by newlines
            var lines = headers.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var parts = line.Split(':', 2, StringSplitOptions.TrimEntries);
                if (parts.Length == 2 && !IsContentHeader(parts[0]))
                {
                    request.Headers.TryAddWithoutValidation(parts[0], parts[1]);
                }
            }
        }
    }

    private static bool IsContentHeader(string headerName)
    {
        return headerName.Equals("content-type", StringComparison.OrdinalIgnoreCase) ||
               headerName.Equals("content-length", StringComparison.OrdinalIgnoreCase) ||
               headerName.Equals("content-encoding", StringComparison.OrdinalIgnoreCase);
    }

    private static string ApplyAuthentication(HttpRequestMessage request, string authentication, string requestUri)
    {
        try
        {
            var auth = JsonSerializer.Deserialize<RequestAuthentication>(authentication);
            if (auth == null) return requestUri;

            switch (auth.Type)
            {
                case AuthenticationType.BasicAuth:
                    if (!string.IsNullOrWhiteSpace(auth.Username))
                    {
                        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{auth.Username}:{auth.Password ?? ""}"));
                        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
                    }
                    break;

                case AuthenticationType.BearerToken:
                    if (!string.IsNullOrWhiteSpace(auth.Token))
                    {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
                    }
                    break;

                case AuthenticationType.ApiKey:
                    if (!string.IsNullOrWhiteSpace(auth.ApiKeyName) && !string.IsNullOrWhiteSpace(auth.ApiKeyValue))
                    {
                        if (auth.ApiKeyLocation == ApiKeyLocation.Header)
                        {
                            request.Headers.TryAddWithoutValidation(auth.ApiKeyName, auth.ApiKeyValue);
                        }
                        else if (auth.ApiKeyLocation == ApiKeyLocation.QueryParameter)
                        {
                            var separator = requestUri.Contains('?') ? "&" : "?";
                            requestUri = $"{requestUri}{separator}{Uri.EscapeDataString(auth.ApiKeyName)}={Uri.EscapeDataString(auth.ApiKeyValue)}";
                        }
                    }
                    break;
            }
        }
        catch
        {
            // If authentication parsing fails, continue without authentication
        }

        return requestUri;
    }
}