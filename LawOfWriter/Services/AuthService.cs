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
    private readonly ILogger<AuthService> _logger;
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
    private const int TokenExpiryFallbackHours = 6;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public AuthService(HttpClient httpClient, IJSRuntime jsRuntime, ILogger<AuthService> logger)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
        _logger = logger;
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed");
            return false;
        }
    }

    /// <summary>
    /// Parst den exp-Claim aus dem JWT-Payload (ohne Signaturvalidierung).
    /// Gibt null zurück wenn das Parsen fehlschlägt.
    /// </summary>
    private DateTime? ParseJwtExpiry(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3)
                return null;

            var payload = parts[1];
            // Base64url → Base64
            payload = payload.Replace('-', '+').Replace('_', '/');
            switch (payload.Length % 4)
            {
                case 2: payload += "=="; break;
                case 3: payload += "="; break;
            }

            var bytes = Convert.FromBase64String(payload);
            using var doc = JsonDocument.Parse(bytes);
            if (doc.RootElement.TryGetProperty("exp", out var expElement))
            {
                var epoch = expElement.GetInt64();
                return DateTimeOffset.FromUnixTimeSeconds(epoch).UtcDateTime;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse JWT expiry from token");
        }
        return null;
    }

    private async Task SaveLoginResponseAsync(LoginResponseDto loginResponse)
    {
        // Lese das echte Ablaufdatum aus dem JWT statt es client-seitig zu berechnen
        var expiry = ParseJwtExpiry(loginResponse.Token) 
                     ?? DateTime.UtcNow.AddHours(TokenExpiryFallbackHours);
        _logger.LogInformation("Saving token with expiry {Expiry:O} (UTC)", expiry);
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
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get token");
            return null;
        }
    }

    public async Task<bool> RefreshTokenAsync(bool forceRefresh = false)
    {
        // If a refresh is already in progress, wait for it and return its result
        await _refreshLock.WaitAsync();
        try
        {
            var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", TokenKey);
            var expiryString = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", TokenExpiryKey);

            // Double-check: another caller may have already refreshed while we were waiting.
            // Skip this check when forceRefresh is true (z.B. nach einem 401 vom Server).
            if (!forceRefresh
                && !string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(expiryString)
                && DateTime.TryParse(expiryString, out var expiry) && DateTime.UtcNow <= expiry)
            {
                _logger.LogDebug("Token still valid after acquiring lock, skipping refresh");
                return true; // Token already refreshed by another concurrent caller
            }

            var refreshToken = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", RefreshTokenKey);

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogWarning("Cannot refresh: token or refresh token is missing");
                return false;
            }

            _logger.LogInformation("Sending token refresh request (forceRefresh: {ForceRefresh})", forceRefresh);

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
                    // Prüfe ob der Server tatsächlich einen neuen Token zurückgegeben hat
                    if (loginResponse.Token == token)
                    {
                        _logger.LogWarning("Refresh endpoint returned the same token — server-side issue. Logging out.");
                        return false;
                    }

                    await SaveLoginResponseAsync(loginResponse);
                    _logger.LogInformation("Token refresh completed successfully");
                    return true;
                }
            }

            _logger.LogWarning("Token refresh failed with status {StatusCode}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh failed");
            return false;
        }
        finally
        {
            _refreshLock.Release();
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
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize roles");
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
        var expiry = ParseJwtExpiry(token) ?? DateTime.UtcNow.AddHours(TokenExpiryFallbackHours);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, token);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenExpiryKey, expiry.ToString("o"));
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "TestApiCallAsync failed");
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
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read localStorage key {Key}", key);
            return null;
        }
    }

    public async Task<string?> GetImageBaseAsync()
    {
        return await GetStorageValueAsync(ImageBase);
    }
}
