using System.ComponentModel.DataAnnotations;
using Companion.Api.Contracts;
using Companion.Api.Security;
using Companion.Core.Abstractions;
using Companion.Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Companion.Api.Controllers;

[ApiController]
[Route("api/approvals")]
[Authorize]
public class ApprovalsController(IApprovalService approvalService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ApprovalRequestResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ApprovalRequestResponse>>> GetApprovals(CancellationToken cancellationToken)
    {
        var approvals = await approvalService.GetApprovalsAsync(User.GetRequiredUserProfileId(), cancellationToken);
        return Ok(approvals.Select(x => x.ToResponse()));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApprovalRequestResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApprovalRequestResponse>> CreateApproval(
        [FromBody] CreateApprovalRequest request,
        CancellationToken cancellationToken)
    {
        var approval = await approvalService.CreateApprovalAsync(
            new CreateApprovalRequestCommand(
                User.GetRequiredUserProfileId(),
                request.ConversationId,
                request.SourceMessageId,
                request.Type,
                request.Reason,
                request.Payload,
                request.RiskLevel ?? "Medium"),
            cancellationToken);

        return Created($"/api/approvals/{approval.Id}", approval.ToResponse());
    }

    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(typeof(ApprovalRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApprovalRequestResponse>> Approve(Guid id, CancellationToken cancellationToken)
    {
        var approval = await approvalService.ApproveAsync(User.GetRequiredUserProfileId(), id, cancellationToken);
        return approval is null ? NotFound() : Ok(approval.ToResponse());
    }

    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(typeof(ApprovalRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApprovalRequestResponse>> Reject(Guid id, CancellationToken cancellationToken)
    {
        var approval = await approvalService.RejectAsync(User.GetRequiredUserProfileId(), id, cancellationToken);
        return approval is null ? NotFound() : Ok(approval.ToResponse());
    }
}

public sealed class CreateApprovalRequest
{
    [Required]
    [MaxLength(100)]
    public string Type { get; init; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Reason { get; init; } = string.Empty;

    [Required]
    [MaxLength(20000)]
    public string Payload { get; init; } = string.Empty;

    public Guid? ConversationId { get; init; }

    public Guid? SourceMessageId { get; init; }

    [MaxLength(32)]
    public string? RiskLevel { get; init; }
}
