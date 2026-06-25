using Companion.Core.Entities;
using Companion.Core.Models;

namespace Companion.Core.Abstractions;

public interface IJwtTokenService
{
    Task<JwtAccessToken> CreateTokenAsync(
        ApplicationUser user,
        UserProfile profile,
        CancellationToken cancellationToken = default);
}
