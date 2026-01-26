using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace LawOfWriter.Services;

public class ApiAuthorizationHandler : DelegatingHandler
{
    private readonly AuthService _authService;
    private readonly ILogger<ApiAuthorizationHandler> _logger;

    public ApiAuthorizationHandler(AuthService authService, ILogger<ApiAuthorizationHandler> logger)
    {
        _authService = authService;
        _logger = logger;
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

        return await base.SendAsync(request, cancellationToken);
    }
}
