using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;

namespace LawOfWriter.Services;

public class ApiAuthorizationHandler : DelegatingHandler
{
    private readonly AuthService _authService;
    private readonly ILogger<ApiAuthorizationHandler> _logger;
    private readonly AuthenticationStateProvider _authStateProvider;

    public ApiAuthorizationHandler(
        AuthService authService, 
        ILogger<ApiAuthorizationHandler> logger,
        AuthenticationStateProvider authStateProvider)
    {
        _authService = authService;
        _logger = logger;
        _authStateProvider = authStateProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _authService.GetTokenAsync();
        
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _logger.LogDebug("Bearer token added to request: {Method} {Uri}", 
                request.Method, request.RequestUri);
        }
        else
        {
            _logger.LogWarning("No valid token available for request: {Method} {Uri}", 
                request.Method, request.RequestUri);
        }

        // Buffer the request body before sending so it can be replayed on a 401 retry.
        // HttpContent streams are read-once; without buffering the retry would send an empty body.
        byte[]? bufferedBody = null;
        string? contentType = null;
        if (request.Content != null)
        {
            bufferedBody = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            contentType = request.Content.Headers.ContentType?.ToString();
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("API returned 401 Unauthorized for {Method} {Uri}. Attempting token refresh.", 
                request.Method, request.RequestUri);
            
            var refreshed = await _authService.RefreshTokenAsync(forceRefresh: true);
            
            if (refreshed)
            {
                _logger.LogInformation("Token refresh successful. Retrying original request.");
                
                var newToken = await _authService.GetTokenAsync();
                if (!string.IsNullOrEmpty(newToken))
                {
                    var newRequest = CloneRequest(request, bufferedBody, contentType);
                    newRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
                    return await base.SendAsync(newRequest, cancellationToken);
                }
            }

            _logger.LogWarning("Token refresh failed or no new token. Logging out user.");
            await _authService.LogoutAsync();
            
            if (_authStateProvider is CustomAuthStateProvider customAuthStateProvider)
            {
                customAuthStateProvider.NotifyUserLogout();
            }
        }

        return response;
    }

    private static HttpRequestMessage CloneRequest(HttpRequestMessage request, byte[]? bufferedBody, string? contentType)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version
        };

        if (bufferedBody != null)
        {
            clone.Content = new ByteArrayContent(bufferedBody);
            if (!string.IsNullOrEmpty(contentType))
                clone.Content.Headers.TryAddWithoutValidation("Content-Type", contentType);
        }

        foreach (var prop in request.Options)
        {
            clone.Options.Set(new HttpRequestOptionsKey<object?>(prop.Key), prop.Value);
        }

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }
}
