using Companion.Core.Entities;

namespace Companion.Core.Models;

public sealed record ChiefOfStaffAnalysisResult(
    IReadOnlyList<OpenLoop> CreatedOpenLoops,
    IReadOnlyList<GoalSuggestion> GoalSuggestions,
    IReadOnlyList<ProjectSuggestion> ProjectSuggestions,
    IReadOnlyList<CompanionInsight> Insights);
