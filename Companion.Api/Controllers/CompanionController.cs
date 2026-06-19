using Companion.Api.Contracts;
using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Companion.Api.Controllers;

[ApiController]
[Route("api/companion")]
public class CompanionController(
    ITaskService taskService,
    IApprovalService approvalService,
    IMemoryService memoryService,
    IConversationService conversationService,
    TimeProvider timeProvider) : ControllerBase
{
    [HttpGet("briefing")]
    [ProducesResponseType(typeof(CompanionBriefingResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CompanionBriefingResponse>> GetBriefing(CancellationToken cancellationToken)
    {
        var userProfileId = CompanionDefaults.LocalUserProfileId;
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var openTasks = await taskService.GetOpenTasksAsync(userProfileId, cancellationToken);
        var pendingApprovals = await approvalService.GetPendingApprovalsAsync(userProfileId, cancellationToken);
        var memories = await memoryService.GetMemoriesAsync(userProfileId, cancellationToken);
        var conversation = await conversationService.GetOrCreateDefaultConversationAsync(userProfileId, cancellationToken);
        var recentMessages = await conversationService.GetRecentMessagesAsync(conversation.Id, 10, cancellationToken);

        var recentMemories = memories
            .Where(x => !x.IsArchived && (x.ExpiresUtc is null || x.ExpiresUtc > now))
            .Take(5)
            .Select(x => x.ToResponse())
            .ToList();

        return Ok(new CompanionBriefingResponse(
            openTasks.Select(x => x.ToResponse()).ToList(),
            pendingApprovals.Select(x => x.ToResponse()).ToList(),
            recentMemories,
            recentMessages.Select(x => x.ToResponse()).ToList()));
    }
}
