using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace LawOfWriter.Services;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly AuthService _authService;
    private readonly ClaimsPrincipal _anonymous = new(new ClaimsIdentity());

    public CustomAuthStateProvider(AuthService authService)
    {
        _authService = authService;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _authService.GetTokenAsync();

        if (string.IsNullOrEmpty(token))
        {
            return new AuthenticationState(_anonymous);
        }

        _authService.SetAuthorizationHeader(token);

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "User")
        }, "jwt");

        var user = new ClaimsPrincipal(identity);

        return new AuthenticationState(user);
    }

    public void NotifyUserAuthentication()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "User")
        }, "jwt");

        var user = new ClaimsPrincipal(identity);

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    public void NotifyUserLogout()
    {
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
    }
}
