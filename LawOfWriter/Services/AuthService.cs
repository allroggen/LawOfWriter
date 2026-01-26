using System.Net.Http.Headers;
using System.Net.Http.Json;
using LawOfWriter.Models;
using Microsoft.JSInterop;

namespace LawOfWriter.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private const string TokenKey = "authToken";
    private const string TokenExpiryKey = "authTokenExpiry";
    private const string ApiBaseUrl = "https://die.sinnnlosen.de/api";
    private const int TokenExpiryHours = 12; // Token läuft nach 12 Stunden ab

    public AuthService(HttpClient httpClient, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
    }

    public async Task<bool> LoginAsync(string username, string password)
    {
        try
        {
            var loginRequest = new LoginRequest
            {
                Username = username,
                Password = password
            };

            var response = await _httpClient.PostAsJsonAsync($"{ApiBaseUrl}/auth/login", loginRequest);

            if (response.IsSuccessStatusCode)
            {
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (loginResponse != null && !string.IsNullOrEmpty(loginResponse.Token))
                {
                    await SetTokenAsync(loginResponse.Token);
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

    public async Task<string?> GetTokenAsync()
    {
        try
        {
            var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", TokenKey);
            if (string.IsNullOrEmpty(token))
                return null;

            // Prüfe ob Token abgelaufen ist
            var expiryString = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", TokenExpiryKey);
            if (!string.IsNullOrEmpty(expiryString) && DateTime.TryParse(expiryString, out var expiry))
            {
                if (DateTime.UtcNow > expiry)
                {
                    // Token ist abgelaufen, entferne ihn
                    await LogoutAsync();
                    return null;
                }
            }

            return token;
        }
        catch
        {
            return null;
        }
    }

    public async Task SetTokenAsync(string token)
    {
        var expiry = DateTime.UtcNow.AddHours(TokenExpiryHours);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, token);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenExpiryKey, expiry.ToString("o")); // ISO 8601 Format
        SetAuthorizationHeader(token);
    }

    public async Task LogoutAsync()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenExpiryKey);
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    public void SetAuthorizationHeader(string token)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<TestDataResponse?> GetTestDataAsync()
    {
        try
        {
            var token = await GetTokenAsync();
            if (string.IsNullOrEmpty(token))
                return null;

            SetAuthorizationHeader(token);
            
            var response = await _httpClient.GetAsync($"{ApiBaseUrl}/data/test");
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<TestDataResponse>();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await GetTokenAsync();
        return !string.IsNullOrEmpty(token);
    }
}
