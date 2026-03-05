using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LawOfWriter.DTO;
using LawOfWriter.Models;
using Microsoft.JSInterop;

namespace LawOfWriter.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private const string TokenKey = "authToken";
    private const string RefreshTokenKey = "authRefreshToken";
    private const string TokenExpiryKey = "authTokenExpiry";
    private const string UserIdKey = "authUserId";
    private const string UserNameKey = "authUserName";
    private const string EmailKey = "authEmail";
    private const string NameKey = "authName";
    private const string VornameKey = "authVorname";
    private const string BDayKey = "authBDay";
    private const string NicknameKey = "authNickname";
    private const string IsGuestKey = "authIsGuest";
    private const string RolesKey = "authRoles";
    private const string ImageBase = "imagebase";
    private const string ApiBaseUrl = "https://die.sinnnlosen.de/api";
    private const int TokenExpiryHours = 6;
    private bool _isRefreshing = false;

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
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
                if (loginResponse != null && !string.IsNullOrEmpty(loginResponse.Token))
                {
                    await SaveLoginResponseAsync(loginResponse);
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

    private async Task SaveLoginResponseAsync(LoginResponseDto loginResponse)
    {
        var expiry = DateTime.UtcNow.AddHours(TokenExpiryHours);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, loginResponse.Token);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", RefreshTokenKey, loginResponse.RefreshToken);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenExpiryKey, expiry.ToString("o"));
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", UserIdKey, loginResponse.Id);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", UserNameKey, loginResponse.UserName ?? "");
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", EmailKey, loginResponse.Email ?? "");
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", NameKey, loginResponse.Name ?? "");
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", VornameKey, loginResponse.Vorname ?? "");
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", BDayKey, loginResponse.BDay?.ToString("o") ?? "");
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", NicknameKey, loginResponse.Nickname ?? "");
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", IsGuestKey, loginResponse.IsGuest.ToString());
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", ImageBase, loginResponse.Bild ?? "");
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", RolesKey, JsonSerializer.Serialize(loginResponse.Roles));
        SetAuthorizationHeader(loginResponse.Token);
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
                    // Versuche Refresh Token statt sofortigem Logout
                    var refreshed = await RefreshTokenAsync();
                    if (!refreshed)
                    {
                        await LogoutAsync();
                        return null;
                    }
                    
                    // Token erneut laden nach Refresh
                    return await _jsRuntime.InvokeAsync<string>("localStorage.getItem", TokenKey);
                }
            }

            return token;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> RefreshTokenAsync()
    {
        if (_isRefreshing) return false;
        
        try
        {
            _isRefreshing = true;
            var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", TokenKey);
            var refreshToken = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", RefreshTokenKey);

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(refreshToken))
                return false;

            var refreshRequest = new RefreshTokenRequest
            {
                Token = token,
                RefreshToken = refreshToken
            };

            var response = await _httpClient.PostAsJsonAsync($"{ApiBaseUrl}/auth/refresh", refreshRequest);

            if (response.IsSuccessStatusCode)
            {
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
                if (loginResponse != null && !string.IsNullOrEmpty(loginResponse.Token))
                {
                    await SaveLoginResponseAsync(loginResponse);
                    return true;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    public async Task<string?> GetUserIdAsync()
        => await GetStorageValueAsync(UserIdKey);

    public async Task<string?> GetUserNameAsync()
        => await GetStorageValueAsync(UserNameKey);

    public async Task<string?> GetEmailAsync()
        => await GetStorageValueAsync(EmailKey);

    public async Task<string?> GetNameAsync()
        => await GetStorageValueAsync(NameKey);

    public async Task<string?> GetVornameAsync()
        => await GetStorageValueAsync(VornameKey);

    public async Task<DateTime?> GetBDayAsync()
    {
        var value = await GetStorageValueAsync(BDayKey);
        if (!string.IsNullOrEmpty(value) && DateTime.TryParse(value, out var bDay))
            return bDay;
        return null;
    }

    public async Task<string?> GetNicknameAsync()
        => await GetStorageValueAsync(NicknameKey);

    public async Task<bool> GetIsGuestAsync()
    {
        var value = await GetStorageValueAsync(IsGuestKey);
        return bool.TryParse(value, out var isGuest) && isGuest;
    }

    public async Task<IList<string>> GetRolesAsync()
    {
        var value = await GetStorageValueAsync(RolesKey);
        if (!string.IsNullOrEmpty(value))
        {
            try
            {
                return JsonSerializer.Deserialize<List<string>>(value) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }
        return new List<string>();
    }

    /// <summary>
    /// Gibt alle gespeicherten User-Infos als LoginResponseDto zurück.
    /// </summary>
    public async Task<LoginResponseDto?> GetCurrentUserAsync()
    {
        var token = await GetTokenAsync();
        if (string.IsNullOrEmpty(token))
            return null;

        return new LoginResponseDto
        {
            Token = token,
            Id = await GetUserIdAsync() ?? "",
            UserName = await GetUserNameAsync(),
            Email = await GetEmailAsync(),
            Name = await GetNameAsync(),
            Vorname = await GetVornameAsync(),
            BDay = await GetBDayAsync(),
            Nickname = await GetNicknameAsync(),
            IsGuest = await GetIsGuestAsync(),
            Roles = await GetRolesAsync()
        };
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
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", RefreshTokenKey);
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenExpiryKey);
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", UserIdKey);
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", UserNameKey);
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", EmailKey);
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", NameKey);
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", VornameKey);
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", BDayKey);
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", NicknameKey);
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", IsGuestKey);
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", RolesKey);
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", ImageBase);
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

    private async Task<string?> GetStorageValueAsync(string key)
    {
        try
        {
            var value = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", key);
            return string.IsNullOrEmpty(value) ? null : value;
        }
        catch
        {
            return null;
        }
    }

    public async Task<string?> GetImageBaseAsync()
    {
        return await GetStorageValueAsync(ImageBase);
    }
}
