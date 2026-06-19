using Companion.Core.Abstractions;
using Companion.Core.Entities;
using Companion.Core.Enums;
using Companion.Core.Models;
using Companion.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Companion.Infrastructure.Services;

public class ApprovalService(CompanionDbContext dbContext, TimeProvider timeProvider) : IApprovalService
{
    public async Task<IReadOnlyList<ApprovalRequest>> GetApprovalsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.ApprovalRequests
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ApprovalRequest>> GetPendingApprovalsAsync(
        Guid? userProfileId = null,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.ApprovalRequests
            .AsNoTracking()
            .Where(x =>
                x.Status == ApprovalRequestStatus.Pending &&
                (userProfileId == null || x.UserProfileId == userProfileId))
            .OrderByDescending(x => x.CreatedUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<ApprovalRequest> CreateApprovalAsync(
        CreateApprovalRequestCommand command,
        CancellationToken cancellationToken = default)
    {
        var approvalRequest = new ApprovalRequest
        {
            Id = Guid.NewGuid(),
            UserProfileId = command.UserProfileId,
            ConversationId = command.ConversationId,
            SourceMessageId = command.SourceMessageId,
            Type = command.Type.Trim(),
            Reason = command.Reason.Trim(),
            Payload = command.Payload.Trim(),
            RiskLevel = string.IsNullOrWhiteSpace(command.RiskLevel) ? "Medium" : command.RiskLevel.Trim(),
            Status = ApprovalRequestStatus.Pending,
            CreatedUtc = timeProvider.GetUtcNow().UtcDateTime
        };

        dbContext.ApprovalRequests.Add(approvalRequest);
        await dbContext.SaveChangesAsync(cancellationToken);

        return approvalRequest;
    }

    public Task<ApprovalRequest?> ApproveAsync(Guid approvalRequestId, CancellationToken cancellationToken = default)
    {
        return SetStatusAsync(approvalRequestId, ApprovalRequestStatus.Approved, cancellationToken);
    }

    public Task<ApprovalRequest?> RejectAsync(Guid approvalRequestId, CancellationToken cancellationToken = default)
    {
        return SetStatusAsync(approvalRequestId, ApprovalRequestStatus.Rejected, cancellationToken);
    }

    private async Task<ApprovalRequest?> SetStatusAsync(
        Guid approvalRequestId,
        ApprovalRequestStatus status,
        CancellationToken cancellationToken)
    {
        var approvalRequest = await dbContext.ApprovalRequests
            .FirstOrDefaultAsync(x => x.Id == approvalRequestId, cancellationToken);

        if (approvalRequest is null)
        {
            return null;
        }

        approvalRequest.Status = status;
        approvalRequest.ReviewedUtc = timeProvider.GetUtcNow().UtcDateTime;

        await dbContext.SaveChangesAsync(cancellationToken);
        return approvalRequest;
    }
}
