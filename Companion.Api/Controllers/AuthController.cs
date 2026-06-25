using System.ComponentModel.DataAnnotations;
using Companion.Api.Contracts;
using Companion.Api.Security;
using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Companion.Core.Entities;
using Companion.Core.Models;
using Companion.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Companion.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    CompanionDbContext dbContext,
    IUserPreferenceService userPreferenceService,
    IJwtTokenService jwtTokenService,
    IAuditService auditService) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthSessionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthSessionResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var applicationUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            DisplayName = request.DisplayName.Trim(),
            CreatedUtc = DateTime.UtcNow
        };

        var createResult = await userManager.CreateAsync(applicationUser, request.Password);
        if (!createResult.Succeeded)
        {
            AddIdentityErrors(createResult);
            return ValidationProblem(ModelState);
        }

        var roleResult = await userManager.AddToRoleAsync(applicationUser, SystemRoles.User);
        if (!roleResult.Succeeded)
        {
            AddIdentityErrors(roleResult);
            return ValidationProblem(ModelState);
        }

        var profile = new UserProfile
        {
            Id = applicationUser.Id,
            ApplicationUserId = applicationUser.Id,
            DisplayName = applicationUser.DisplayName,
            Email = applicationUser.Email ?? request.Email.Trim(),
            CreatedUtc = applicationUser.CreatedUtc,
            UpdatedUtc = applicationUser.CreatedUtc
        };

        dbContext.UserProfiles.Add(profile);
        await dbContext.SaveChangesAsync(cancellationToken);

        await userPreferenceService.SetPreferenceAsync(profile.Id, "ResponseStyle", "Balanced", cancellationToken);
        await userPreferenceService.SetPreferenceAsync(profile.Id, "Notifications", "ImportantOnly", cancellationToken);
        await userPreferenceService.SetPreferenceAsync(profile.Id, "AiPersonality", "SupportivePragmatic", cancellationToken);
        await auditService.WriteEventAsync(
            profile.Id,
            "Registration",
            nameof(ApplicationUser),
            applicationUser.Id.ToString(),
            "Registered a new account.",
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        var token = await jwtTokenService.CreateTokenAsync(applicationUser, profile, cancellationToken);
        return Created("/api/auth/me", await BuildSessionResponseAsync(applicationUser, profile, token, cancellationToken));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthSessionResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var applicationUser = await userManager.Users
            .FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);

        if (applicationUser is null || !await userManager.CheckPasswordAsync(applicationUser, request.Password))
        {
            return Unauthorized();
        }

        var profile = await dbContext.UserProfiles
            .AsNoTracking()
            .FirstAsync(x => x.ApplicationUserId == applicationUser.Id, cancellationToken);

        applicationUser.LastLoginUtc = DateTime.UtcNow;
        await userManager.UpdateAsync(applicationUser);
        await auditService.WriteEventAsync(
            profile.Id,
            AuditEventTypes.Login,
            nameof(ApplicationUser),
            applicationUser.Id.ToString(),
            "Logged in successfully.",
            cancellationToken);

        var token = await jwtTokenService.CreateTokenAsync(applicationUser, profile, cancellationToken);
        return Ok(await BuildSessionResponseAsync(applicationUser, profile, token, cancellationToken));
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        var userProfileId = User.GetRequiredUserProfileId();
        var applicationUser = await userManager.FindByIdAsync(userId.ToString());
        if (applicationUser is not null)
        {
            await userManager.UpdateSecurityStampAsync(applicationUser);
            await auditService.WriteEventAsync(
                userProfileId,
                AuditEventTypes.Logout,
                nameof(ApplicationUser),
                applicationUser.Id.ToString(),
                "Logged out and invalidated existing tokens.",
                cancellationToken);
        }

        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CurrentUserResponse>> Me(CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        var applicationUser = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("Authenticated user record was not found.");
        var profile = await dbContext.UserProfiles
            .AsNoTracking()
            .FirstAsync(x => x.ApplicationUserId == applicationUser.Id, cancellationToken);

        return Ok(await BuildCurrentUserResponseAsync(applicationUser, profile, cancellationToken));
    }

    private async Task<AuthSessionResponse> BuildSessionResponseAsync(
        ApplicationUser applicationUser,
        UserProfile profile,
        JwtAccessToken token,
        CancellationToken cancellationToken)
    {
        var me = await BuildCurrentUserResponseAsync(applicationUser, profile, cancellationToken);
        return new AuthSessionResponse(token.AccessToken, token.ExpiresUtc, me);
    }

    private async Task<CurrentUserResponse> BuildCurrentUserResponseAsync(
        ApplicationUser applicationUser,
        UserProfile profile,
        CancellationToken cancellationToken)
    {
        var roles = await userManager.GetRolesAsync(applicationUser);
        var preferences = await userPreferenceService.GetPreferencesAsync(profile.Id, cancellationToken);
        var isAdministrator = roles.Contains(SystemRoles.Administrator, StringComparer.Ordinal);

        return new CurrentUserResponse(
            new AuthUserProfileResponse(
                applicationUser.Id,
                profile.Id,
                applicationUser.Email ?? profile.Email,
                applicationUser.DisplayName,
                applicationUser.CreatedUtc,
                applicationUser.LastLoginUtc,
                roles.ToList()),
            preferences.Select(x => x.ToResponse()).ToList(),
            new UserCapabilitiesResponse(
                CanManageOwnData: true,
                CanQueueAgentRuns: true,
                CanReviewApprovals: true,
                CanManageAiSettings: isAdministrator,
                CanViewAuditTrail: true));
    }

    private void AddIdentityErrors(IdentityResult identityResult)
    {
        foreach (var error in identityResult.Errors)
        {
            ModelState.AddModelError(error.Code, error.Description);
        }
    }
}

public sealed class RegisterRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(320)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(200)]
    public string Password { get; init; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string DisplayName { get; init; } = string.Empty;
}

public sealed class LoginRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(320)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Password { get; init; } = string.Empty;
}
