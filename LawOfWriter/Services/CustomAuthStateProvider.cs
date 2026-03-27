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
        var currentUser = await _authService.GetCurrentUserAsync();

        if (currentUser == null || string.IsNullOrEmpty(currentUser.Token))
        {
            return new AuthenticationState(_anonymous);
        }

        _authService.SetAuthorizationHeader(currentUser.Token);

        var claims = BuildClaims(currentUser);
        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);

        return new AuthenticationState(user);
    }

    public async Task NotifyUserAuthentication()
    {
        var currentUser = await _authService.GetCurrentUserAsync();
        var claims = currentUser != null ? BuildClaims(currentUser) : new List<Claim> { new(ClaimTypes.Name, "User") };
        var identity = new ClaimsIdentity(claims, "jwt");
        var user = new ClaimsPrincipal(identity);

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    private static List<Claim> BuildClaims(DTO.LoginResponseDto user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.Nickname ?? user.UserName ?? "User"),
        };

        if (!string.IsNullOrEmpty(user.Email))
            claims.Add(new Claim(ClaimTypes.Email, user.Email));

        if (!string.IsNullOrEmpty(user.Name))
            claims.Add(new Claim(ClaimTypes.Surname, user.Name));

        if (!string.IsNullOrEmpty(user.Vorname))
            claims.Add(new Claim(ClaimTypes.GivenName, user.Vorname));

        if (user.BDay.HasValue)
            claims.Add(new Claim(ClaimTypes.DateOfBirth, user.BDay.Value.ToString("o")));

        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        return claims;
    }

    public void NotifyUserLogout()
    {
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
    }
}
