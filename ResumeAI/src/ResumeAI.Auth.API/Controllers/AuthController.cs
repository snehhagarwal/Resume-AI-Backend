using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResumeAI.Auth.API.Services;
using ResumeAI.Shared.DTOs;
using ResumeAI.Shared.Enums;
using ResumeAI.Auth.API.Interfaces;

namespace ResumeAI.Auth.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Missing user claim."));

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var response = await authService.RegisterAsync(request);
            return Ok(ApiResponse<AuthResponse>.Ok(response));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<AuthResponse>.Fail(ex.Message));
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var response = await authService.LoginAsync(request);
            return Ok(ApiResponse<AuthResponse>.Ok(response));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<AuthResponse>.Fail(ex.Message));
        }
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await authService.LogoutAsync(CurrentUserId);
        return NoContent();
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] string refreshToken)
    {
        try
        {
            var token = await authService.RefreshTokenAsync(refreshToken);
            return Ok(ApiResponse<string>.Ok(token));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<string>.Fail(ex.Message));
        }
        catch (NotImplementedException ex)
        {
            return StatusCode(501, ApiResponse<string>.Fail(ex.Message));
        }
    }

    [HttpGet("validate")]
    public IActionResult ValidateToken([FromQuery] string token)
    {
        var isValid = authService.ValidateToken(token);
        return Ok(ApiResponse<bool>.Ok(isValid));
    }

    [Authorize]
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var user = await authService.GetUserByIdAsync(CurrentUserId);
        return user is null ? NotFound() : Ok(ApiResponse<UserDto>.Ok(user));
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var user = await authService.UpdateProfileAsync(CurrentUserId, request);
        return Ok(ApiResponse<UserDto>.Ok(user));
    }

    [Authorize]
    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            await authService.ChangePasswordAsync(CurrentUserId, request);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<string>.Fail(ex.Message));
        }
    }

    [Authorize]
    [HttpPut("subscription")]
    public async Task<IActionResult> UpdateSubscription([FromBody] UpdateSubscriptionRequest request)
    {
        await authService.UpdateSubscriptionAsync(CurrentUserId, request.Plan);
        return NoContent();
    }

    [Authorize]
    [HttpDelete("deactivate")]
    public async Task<IActionResult> Deactivate()
    {
        await authService.DeactivateAccountAsync(CurrentUserId);
        return NoContent();
    }

    // Admin-only
    [Authorize(Roles = "ADMIN")]
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers([FromQuery] Role? role)
    {
        var users = await authService.GetAllUsersAsync();
        if (role.HasValue)
        {
            users = users.Where(u => u.Role == role.Value).ToList();
        }
        return Ok(ApiResponse<IList<UserDto>>.Ok(users));
    }

    [Authorize(Roles = "ADMIN")]
    [HttpGet("users/{userId:int}")]
    public async Task<IActionResult> AdminGetUser(int userId)
    {
        var user = await authService.GetUserByIdAsync(userId);
        return user is null ? NotFound() : Ok(ApiResponse<UserDto>.Ok(user));
    }

    [Authorize(Roles = "ADMIN")]
    [HttpPut("users/{userId:int}/subscription")]
    public async Task<IActionResult> AdminUpdateSubscription(int userId, [FromBody] UpdateSubscriptionRequest request)
    {
        await authService.UpdateSubscriptionAsync(userId, request.Plan);
        return NoContent();
    }

    [Authorize(Roles = "ADMIN")]
    [HttpDelete("users/{userId:int}")]
    public async Task<IActionResult> AdminDeleteUser(int userId)
    {
        await authService.HardDeleteUserAsync(userId);
        return NoContent();
    }

    [Authorize(Roles = "ADMIN")]
    [HttpPut("users/{userId:int}/reactivate")]
    public async Task<IActionResult> AdminReactivateUser(int userId)
    {
        await authService.ReactivateAccountAsync(userId);
        return NoContent();
    }

    [Authorize(Roles = "ADMIN")]
    [HttpPut("users/{userId:int}/suspend")]
    public async Task<IActionResult> AdminSuspendUser(int userId)
    {
        await authService.SuspendUserAsync(userId);
        return NoContent();
    }

    // ─── Google OAuth2 ────────────────────────────────────────────

    /// <summary>
    /// Redirects the browser to Google's OAuth2 consent screen.
    /// Supply an optional <paramref name="returnUrl"/> to send the user
    /// back to the right page after a successful login.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("oauth/google")]
    public IActionResult LoginWithGoogle([FromQuery] string? returnUrl = "/")
    {
        var props = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(OAuthCallback), new { provider = "google" }),
            Items       = { ["returnUrl"] = returnUrl }
        };
        return Challenge(props, "Google");
    }

    // ─── LinkedIn OAuth2 ──────────────────────────────────────────

    /// <summary>
    /// Redirects the browser to LinkedIn's OAuth2 consent screen.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("oauth/linkedin")]
    public IActionResult LoginWithLinkedIn([FromQuery] string? returnUrl = "/")
    {
        var props = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(OAuthCallback), new { provider = "linkedin" }),
            Items       = { ["returnUrl"] = returnUrl }
        };
        return Challenge(props, "LinkedIn");
    }

    // ─── Shared OAuth Callback ────────────────────────────────────

    /// <summary>
    /// Shared callback hit after the OAuth provider redirects back.
    /// The OAuth middleware has already validated the code and written
    /// the external identity into a short-lived cookie.  We read that
    /// cookie here, find-or-create the application user, issue our
    /// own JWT, then redirect the browser back to the React SPA with
    /// the token in the query-string so AuthContext can store it.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("oauth/{provider}/callback")]
    public async Task<IActionResult> OAuthCallback(string provider)
    {
        var result = await HttpContext.AuthenticateAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        // The frontend URL — reads from config, falls back to localhost:3000 for dev.
        var frontendBase = HttpContext.RequestServices
            .GetRequiredService<IConfiguration>()["Frontend:BaseUrl"]
            ?? "http://localhost:3000";

        if (!result.Succeeded || result.Principal is null)
            return Redirect($"{frontendBase}/auth/callback?error=oauth_failed");

        var principal = result.Principal;

        var email = principal.FindFirstValue(ClaimTypes.Email)
                    ?? principal.FindFirstValue("email");

        var fullName = principal.FindFirstValue(ClaimTypes.Name)
                       ?? principal.FindFirstValue("name")
                       ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email))
            return Redirect($"{frontendBase}/auth/callback?error=no_email");

        var authProvider = provider.ToLowerInvariant() switch
        {
            "google"   => AuthProvider.GOOGLE,
            "linkedin" => AuthProvider.LINKEDIN,
            _          => throw new ArgumentOutOfRangeException(
                              nameof(provider), $"Unknown provider: {provider}")
        };

        try
        {
            var response = await authService.OAuthLoginAsync(authProvider, email, fullName);

            // Discard the transient cookie — client now holds our JWT.
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Redirect back to React SPA.  The /auth/callback page stores the token
            // in localStorage and navigates the user to their destination.
            var returnUrl = result.Properties?.Items.TryGetValue("returnUrl", out var ru) == true
                ? ru ?? "/"
                : "/";

            return Redirect($"{frontendBase}/auth/callback?token={Uri.EscapeDataString(response.Token)}&returnUrl={Uri.EscapeDataString(returnUrl)}");
        }
        catch (Exception)
        {
            return Redirect($"{frontendBase}/auth/callback?error=login_failed");
        }
    }
}
