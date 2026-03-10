using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace PropertyManagement.Auth;

public class CookieAuthStateProvider : AuthenticationStateProvider
{
    private readonly Task<AuthenticationState> _authState;

    public CookieAuthStateProvider(IHttpContextAccessor httpContextAccessor)
    {
        // Capture the user ONCE during scope creation (constructor runs when the
        // circuit scope is initialised, while HttpContext is still accessible).
        // Calling HttpContext.User lazily inside GetAuthenticationStateAsync()
        // fails because IHttpContextAccessor returns null once rendering moves
        // to the interactive WebSocket phase.
        var user = httpContextAccessor.HttpContext?.User
                   ?? new ClaimsPrincipal(new ClaimsIdentity());
        _authState = Task.FromResult(new AuthenticationState(user));
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync() => _authState;
}
