using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PropertyManagement.Services;
using System.Security.Claims;

namespace PropertyManagement.Pages.Account;

public class ExternalLoginModel : PageModel
{
    private readonly IUserService _userService;

    public ExternalLoginModel(IUserService userService) => _userService = userService;

    public string ErrorMessage { get; set; } = string.Empty;

    // Step 1: User clicked "Sign in with Google" — kick off the OAuth challenge
    public IActionResult OnPost(string provider, string? returnUrl = null)
    {
        var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, provider);
    }

    // Step 2: Google redirects back here after the user authenticates
    public async Task<IActionResult> OnGetCallbackAsync(string? returnUrl = null, string? remoteError = null)
    {
        if (remoteError != null)
        {
            ErrorMessage = $"Google sign-in error: {remoteError}";
            return Page();
        }

        var result = await HttpContext.AuthenticateAsync("ExternalCookie");
        if (!result.Succeeded)
        {
            ErrorMessage = "Could not retrieve account information from Google.";
            return Page();
        }

        var email = result.Principal?.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrEmpty(email))
        {
            ErrorMessage = "No email address was provided by Google.";
            await HttpContext.SignOutAsync("ExternalCookie");
            return Page();
        }

        var user = await _userService.GetByEmailAsync(email);
        if (user == null)
        {
            ErrorMessage = $"No account exists for {email}. Contact your administrator to be added to the system.";
            await HttpContext.SignOutAsync("ExternalCookie");
            return Page();
        }

        await _userService.UpdateLastLoginAsync(user.Id);
        await HttpContext.SignOutAsync("ExternalCookie");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) });

        return LocalRedirect(returnUrl ?? "/");
    }
}
