namespace Companion.Core.Entities;

public class ToolPermission
{
    public Guid Id { get; set; }

    public Guid UserProfileId { get; set; }

    public Guid ToolDefinitionId { get; set; }

    public bool Allowed { get; set; }

    public DateTime CreatedUtc { get; set; }

    public UserProfile? UserProfile { get; set; }

    public ToolDefinition? ToolDefinition { get; set; }
}
