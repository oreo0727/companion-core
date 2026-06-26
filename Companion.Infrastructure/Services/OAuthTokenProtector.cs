using Companion.Core.Abstractions;
using Microsoft.AspNetCore.DataProtection;

namespace Companion.Infrastructure.Services;

public class OAuthTokenProtector(IDataProtectionProvider dataProtectionProvider) : IOAuthTokenProtector
{
    private readonly IDataProtector protector = dataProtectionProvider.CreateProtector("companion.oauth-tokens.v1");

    public string Protect(string token)
    {
        return protector.Protect(token);
    }

    public string? Unprotect(string? protectedToken)
    {
        if (string.IsNullOrWhiteSpace(protectedToken))
        {
            return null;
        }

        try
        {
            return protector.Unprotect(protectedToken);
        }
        catch
        {
            return null;
        }
    }
}
