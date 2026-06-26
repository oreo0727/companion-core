namespace Companion.Core.Abstractions;

public interface IOAuthTokenProtector
{
    string Protect(string token);

    string? Unprotect(string? protectedToken);
}
