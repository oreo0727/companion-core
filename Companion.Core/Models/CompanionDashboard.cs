namespace Companion.Core.Models;

public sealed record CompanionDashboard(
    int ActiveProjects,
    int ActiveGoals,
    int OpenLoops,
    int PendingApprovals,
    IReadOnlyList<CompanionInsight> TopInsights);
