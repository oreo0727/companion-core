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
    public const string ReminderCreated = "ReminderCreated";
    public const string NotificationRead = "NotificationRead";
    public const string KnowledgeDocumentImported = "KnowledgeDocumentImported";
    public const string KnowledgeSearchPerformed = "KnowledgeSearchPerformed";
    public const string ConnectorConnected = "ConnectorConnected";
    public const string ConnectorDisconnected = "ConnectorDisconnected";
    public const string ConnectorSyncStarted = "ConnectorSyncStarted";
    public const string ConnectorSyncCompleted = "ConnectorSyncCompleted";
    public const string OAuthAuthorizationStarted = "OAuthAuthorizationStarted";
    public const string OAuthConsentGranted = "OAuthConsentGranted";
    public const string OAuthConsentRevoked = "OAuthConsentRevoked";
    public const string CalendarEventsViewed = "CalendarEventsViewed";
    public const string EmailMessagesViewed = "EmailMessagesViewed";
    public const string EmailSearchPerformed = "EmailSearchPerformed";
    public const string FileDocumentsViewed = "FileDocumentsViewed";
    public const string HomeDevicesViewed = "HomeDevicesViewed";
    public const string HomeSensorsViewed = "HomeSensorsViewed";
    public const string HomeActionRequested = "HomeActionRequested";
    public const string HomeActionExecuted = "HomeActionExecuted";
    public const string VoiceSessionStarted = "VoiceSessionStarted";
    public const string VoiceTranscribed = "VoiceTranscribed";
    public const string VoiceSpoken = "VoiceSpoken";
    public const string VoiceInterrupted = "VoiceInterrupted";
    public const string ToolExecutionRequested = "ToolExecutionRequested";
    public const string ToolExecutionCompleted = "ToolExecutionCompleted";
    public const string ToolExecutionFailed = "ToolExecutionFailed";
    public const string ToolExecutionRejected = "ToolExecutionRejected";
}
