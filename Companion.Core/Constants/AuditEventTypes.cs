namespace Companion.Core.Constants;

public static class AuditEventTypes
{
    public const string Login = "Login";
    public const string Logout = "Logout";
    public const string MemoryCreated = "MemoryCreated";
    public const string TaskCreated = "TaskCreated";
    public const string ApprovalApproved = "ApprovalApproved";
    public const string ApprovalRejected = "ApprovalRejected";
    public const string SettingsChanged = "SettingsChanged";
    public const string PreferenceChanged = "PreferenceChanged";
    public const string KnowledgeDocumentImported = "KnowledgeDocumentImported";
    public const string KnowledgeSearchPerformed = "KnowledgeSearchPerformed";
    public const string ToolExecutionRequested = "ToolExecutionRequested";
    public const string ToolExecutionCompleted = "ToolExecutionCompleted";
    public const string ToolExecutionFailed = "ToolExecutionFailed";
    public const string ToolExecutionRejected = "ToolExecutionRejected";
}
