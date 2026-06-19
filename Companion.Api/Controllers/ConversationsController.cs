using Companion.Api.Contracts;
using Companion.Core.Abstractions;
using Companion.Core.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Companion.Api.Controllers;

[ApiController]
[Route("api/conversations")]
public class ConversationsController(IConversationService conversationService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ConversationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ConversationResponse>>> GetConversations(CancellationToken cancellationToken)
    {
        var conversations = await conversationService.GetConversationsAsync(
            CompanionDefaults.LocalUserProfileId,
            cancellationToken);

        return Ok(conversations.Select(x => x.ToResponse()));
    }

    [HttpGet("{id:guid}/messages")]
    [ProducesResponseType(typeof(IEnumerable<MessageResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<MessageResponse>>> GetConversationMessages(
        Guid id,
        CancellationToken cancellationToken)
    {
        var conversation = await conversationService.GetConversationAsync(
            CompanionDefaults.LocalUserProfileId,
            id,
            cancellationToken);

        if (conversation is null)
        {
            return NotFound();
        }

        var messages = await conversationService.GetRecentMessagesAsync(id, 250, cancellationToken);
        return Ok(messages.Select(x => x.ToResponse()));
    }
}
