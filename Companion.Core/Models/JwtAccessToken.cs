namespace Companion.Core.Models;

public sealed record JwtAccessToken(
    string AccessToken,
    DateTime ExpiresUtc);
