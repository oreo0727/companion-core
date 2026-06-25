using Companion.Core.Entities;
using Companion.Core.Models;

namespace Companion.Core.Abstractions;

public interface IApprovalService
{
    Task<IReadOnlyList<ApprovalRequest>> GetApprovalsAsync(
        Guid userProfileId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ApprovalRequest>> GetPendingApprovalsAsync(
        Guid? userProfileId = null,
        CancellationToken cancellationToken = default);

    Task<ApprovalRequest> CreateApprovalAsync(CreateApprovalRequestCommand command, CancellationToken cancellationToken = default);

    Task<ApprovalRequest?> ApproveAsync(
        Guid userProfileId,
        Guid approvalRequestId,
        CancellationToken cancellationToken = default);

    Task<ApprovalRequest?> RejectAsync(
        Guid userProfileId,
        Guid approvalRequestId,
        CancellationToken cancellationToken = default);
}
