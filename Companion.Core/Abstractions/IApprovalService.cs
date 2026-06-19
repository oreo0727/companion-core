using Companion.Core.Entities;
using Companion.Core.Models;

namespace Companion.Core.Abstractions;

public interface IApprovalService
{
    Task<IReadOnlyList<ApprovalRequest>> GetApprovalsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ApprovalRequest>> GetPendingApprovalsAsync(
        Guid? userProfileId = null,
        CancellationToken cancellationToken = default);

    Task<ApprovalRequest> CreateApprovalAsync(CreateApprovalRequestCommand command, CancellationToken cancellationToken = default);

    Task<ApprovalRequest?> ApproveAsync(Guid approvalRequestId, CancellationToken cancellationToken = default);

    Task<ApprovalRequest?> RejectAsync(Guid approvalRequestId, CancellationToken cancellationToken = default);
}
