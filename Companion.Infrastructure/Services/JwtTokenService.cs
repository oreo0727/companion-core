using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Companion.Core.Entities;
using Companion.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Companion.Infrastructure.Services;

public class JwtTokenService(
    IConfiguration configuration,
    UserManager<ApplicationUser> userManager) : IJwtTokenService
{
    public async Task<JwtAccessToken> CreateTokenAsync(
        ApplicationUser user,
        UserProfile profile,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var issuer = configuration["Jwt:Issuer"] ?? "CompanionCore";
        var audience = configuration["Jwt:Audience"] ?? "CompanionCore.Client";
        var signingKey = configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey is not configured.");
        var expiresMinutes = int.TryParse(configuration["Jwt:ExpiresMinutes"], out var configuredMinutes)
            ? Math.Clamp(configuredMinutes, 5, 1440)
            : 480;
        var now = DateTime.UtcNow;
        var expiresUtc = now.AddMinutes(expiresMinutes);
        var roles = await userManager.GetRolesAsync(user);
        var securityStamp = await userManager.GetSecurityStampAsync(user) ?? string.Empty;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(CompanionClaimTypes.UserProfileId, profile.Id.ToString()),
            new(CompanionClaimTypes.DisplayName, user.DisplayName),
            new(CompanionClaimTypes.SecurityStamp, securityStamp),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            notBefore: now,
            expires: expiresUtc,
            signingCredentials: credentials);

        return new JwtAccessToken(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresUtc);
    }
}
