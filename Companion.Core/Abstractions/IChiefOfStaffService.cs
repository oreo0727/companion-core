using Companion.Core.Entities;
using Companion.Core.Models;

namespace Companion.Core.Abstractions;

public interface IChiefOfStaffService
{
    Task<ChiefOfStaffAnalysisResult> AnalyzeMessageAsync(
        Guid userProfileId,
        Message message,
        CancellationToken cancellationToken = default);

    Task<CompanionBriefing> GetBriefingAsync(Guid userProfileId, CancellationToken cancellationToken = default);

    Task<CompanionDashboard> GetDashboardAsync(Guid userProfileId, CancellationToken cancellationToken = default);
}
