using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using PetHub.API.Common;
using PetHub.API.DTOs.Chat;
using PetHub.API.DTOs.Common;
using PetHub.API.Hubs;
using PetHub.API.Services;

namespace PetHub.API.Controllers;

[ApiController]
[Route("api/conversations")]
[Authorize]
public class ConversationsController(IChatService chatService, IHubContext<ChatHub> hubContext)
    : ApiControllerBase
{
    /// <summary>
    /// Gets or creates a conversation about a pet, or linked to an adoption request
    /// </summary>
    /// <param name="dto">Pet ID and/or adoption request ID</param>
    /// <returns>Conversation with the other participant and pet context</returns>
    /// <response code="200">Existing conversation returned</response>
    /// <response code="201">Conversation created</response>
    /// <response code="400">Invalid payload or attempting to chat about own pet</response>
    /// <response code="401">User not authenticated or invalid token</response>
    /// <response code="403">User is not a party of the adoption request</response>
    /// <response code="404">Pet or adoption request not found</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ConversationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ConversationResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> CreateConversation([FromBody] CreateConversationDto dto)
    {
        var userIdResult = GetUserIdOrUnauthorized();
        if (userIdResult.Result is UnauthorizedObjectResult unauthorized)
            return unauthorized;

        var userId = userIdResult.Value;

        return await ExecuteAsync(async () =>
        {
            var (conversation, created) = dto.AdoptionRequestId.HasValue
                ? await chatService.GetOrCreateForAdoptionRequestAsync(
                    dto.AdoptionRequestId.Value,
                    userId
                )
                : await chatService.GetOrCreateForPetAsync(dto.PetId!.Value, userId);

            if (created)
            {
                return CreatedAtAction(
                    nameof(GetConversation),
                    new { id = conversation.Id },
                    conversation
                );
            }

            return Success(conversation);
        });
    }

    /// <summary>
    /// Lists conversations of the authenticated user, newest activity first
    /// </summary>
    /// <returns>Inbox with last message preview and unread counts</returns>
    /// <response code="200">Inbox retrieved successfully</response>
    /// <response code="401">User not authenticated or invalid token</response>
    [HttpGet]
    [ProducesResponseType(
        typeof(ApiResponse<List<ConversationResponseDto>>),
        StatusCodes.Status200OK
    )]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> GetInbox()
    {
        var userIdResult = GetUserIdOrUnauthorized();
        if (userIdResult.Result is UnauthorizedObjectResult unauthorized)
            return unauthorized;

        var userId = userIdResult.Value;
        var inbox = await chatService.GetInboxAsync(userId);

        return Success(inbox);
    }

    /// <summary>
    /// Gets a conversation the authenticated user participates in
    /// </summary>
    /// <param name="id">Conversation ID</param>
    /// <returns>Conversation details</returns>
    /// <response code="200">Conversation retrieved successfully</response>
    /// <response code="401">User not authenticated or invalid token</response>
    /// <response code="403">User is not a participant</response>
    /// <response code="404">Conversation not found</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ConversationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetConversation(int id)
    {
        var userIdResult = GetUserIdOrUnauthorized();
        if (userIdResult.Result is UnauthorizedObjectResult unauthorized)
            return unauthorized;

        var userId = userIdResult.Value;

        return await ExecuteAsync(async () =>
            Success(await chatService.GetConversationAsync(id, userId))
        );
    }

    /// <summary>
    /// Gets persisted message history for a conversation
    /// </summary>
    /// <param name="id">Conversation ID</param>
    /// <param name="beforeId">Optional message ID cursor to load older messages</param>
    /// <param name="pageSize">Page size (default 50, max 100)</param>
    /// <returns>Messages in chronological order</returns>
    /// <response code="200">Messages retrieved successfully</response>
    /// <response code="401">User not authenticated or invalid token</response>
    /// <response code="403">User is not a participant</response>
    /// <response code="404">Conversation not found</response>
    [HttpGet("{id:int}/messages")]
    [ProducesResponseType(
        typeof(ApiResponse<List<ChatMessageResponseDto>>),
        StatusCodes.Status200OK
    )]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetMessages(
        int id,
        [FromQuery] int? beforeId,
        [FromQuery] int pageSize = ChatService.DefaultMessagePageSize
    )
    {
        var userIdResult = GetUserIdOrUnauthorized();
        if (userIdResult.Result is UnauthorizedObjectResult unauthorized)
            return unauthorized;

        var userId = userIdResult.Value;

        return await ExecuteAsync(async () =>
            Success(await chatService.GetMessagesAsync(id, userId, beforeId, pageSize))
        );
    }

    /// <summary>
    /// Sends a message in a conversation and persists it
    /// </summary>
    /// <param name="id">Conversation ID</param>
    /// <param name="dto">Message content</param>
    /// <returns>Saved message</returns>
    /// <response code="200">Message sent successfully</response>
    /// <response code="400">Empty or too long content</response>
    /// <response code="401">User not authenticated or invalid token</response>
    /// <response code="403">User is not a participant</response>
    /// <response code="404">Conversation not found</response>
    [HttpPost("{id:int}/messages")]
    [ProducesResponseType(typeof(ApiResponse<ChatMessageResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> SendMessage(int id, [FromBody] SendMessageDto dto)
    {
        var userIdResult = GetUserIdOrUnauthorized();
        if (userIdResult.Result is UnauthorizedObjectResult unauthorized)
            return unauthorized;

        var userId = userIdResult.Value;

        return await ExecuteAsync(async () =>
        {
            var message = await chatService.SendMessageAsync(id, userId, dto.Content);
            await hubContext
                .Clients.Group(ChatHub.GroupName(id))
                .SendAsync(ChatHub.ReceiveMessageEvent, message);

            return Success(message);
        });
    }

    /// <summary>
    /// Marks messages sent by the other participant as read
    /// </summary>
    /// <param name="id">Conversation ID</param>
    /// <returns>How many messages were marked as read</returns>
    /// <response code="200">Messages marked as read</response>
    /// <response code="401">User not authenticated or invalid token</response>
    /// <response code="403">User is not a participant</response>
    /// <response code="404">Conversation not found</response>
    [HttpPost("{id:int}/read")]
    [ProducesResponseType(typeof(ApiResponse<MessagesReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> MarkAsRead(int id)
    {
        var userIdResult = GetUserIdOrUnauthorized();
        if (userIdResult.Result is UnauthorizedObjectResult unauthorized)
            return unauthorized;

        var userId = userIdResult.Value;

        return await ExecuteAsync(async () =>
        {
            var result = await chatService.MarkAsReadAsync(id, userId);
            await hubContext
                .Clients.Group(ChatHub.GroupName(id))
                .SendAsync(ChatHub.MessagesReadEvent, result);

            return Success(result);
        });
    }

    private async Task<ActionResult> ExecuteAsync(Func<Task<ActionResult>> action)
    {
        try
        {
            return await action();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return ForbiddenResponse(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return Error(ex.Message);
        }
    }
}
