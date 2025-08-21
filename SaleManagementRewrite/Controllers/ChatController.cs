using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaleManagementRewrite.Data;

namespace SaleManagementRewrite.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController(ApiDbContext dbContext) : ControllerBase
{
    [HttpGet("get_chat")]
    public async Task<IActionResult> GetChatHistory(Guid otherUserId)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var currentUserId))
        {
            return Unauthorized();
        }
        var messages = await dbContext.Messages
            .Where(m =>
                m.Conversation != null && ((m.Conversation.ParticipantAId == currentUserId && m.Conversation.ParticipantBId == otherUserId) ||
                                           (m.Conversation.ParticipantAId == otherUserId && m.Conversation.ParticipantBId == currentUserId)))
            .OrderBy(m =>m.SendAt).Select(m => new { m.Context,m.RecipientId,m.SenderId, m.IsRead }).ToListAsync();
        return Ok(messages);
    }
}