using Microsoft.AspNetCore.Identity;

namespace Companion.Core.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; }

    public DateTime? LastLoginUtc { get; set; }

    public UserProfile? UserProfile { get; set; }
}
