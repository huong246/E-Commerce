
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SaleManagementRewrite.Data;
using SaleManagementRewrite.Entities;

namespace SaleManagementRewrite.Hubs;

public class ChatHubs(ApiDbContext dbContext)
    : Hub
{
    public async Task SendMessage(Guid receiveId, string messageContext)
    {
        var userIdString = Context.UserIdentifier;
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return;
        }
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return;
        }
        var receiver = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == receiveId);
        if (receiver == null)
        {
            return;
        }
        var conversation = await dbContext.Conversations.FirstOrDefaultAsync(c => c.ParticipantAId == userId && c.ParticipantBId == receiveId || c.ParticipantAId == receiveId && c.ParticipantBId == userId);
        if (conversation == null)
        {
            conversation = new Conversation()
            {
                Id = Guid.NewGuid(),
                ParticipantAId = userId,
                ParticipantBId = receiveId,
            };
            await dbContext.Conversations.AddAsync(conversation);
        }

        var message = new Message()
        {
            Id = Guid.NewGuid(),
            Context = messageContext,
            SendAt = DateTime.UtcNow,
            ConversationId = conversation.Id,
            Conversation = conversation,
            Recipient = receiver,
            RecipientId = receiveId,
            Sender = user,
            SenderId = userId,
            IsRead = false,
        };
        await dbContext.Messages.AddAsync(message);
        await dbContext.SaveChangesAsync();
        var messageData = new
        {
            message.Id,
            message.Context,
            message.Sender,
            message.SenderId,
            message.IsRead,
            Receiver = message.Recipient,
            ReceiverId = message.RecipientId,
            SenderAt = message.SendAt,
            conversation,
            conversationId = conversation.Id,
        };
        await Clients.Users(userId.ToString()).SendAsync("SendMessage",  userId.ToString(),messageData);
        await Clients.Users(receiveId.ToString()).SendAsync("ReceiveMessage",  userId.ToString(),messageData);
    }
//Tự thêm vào 1 group mã của họ khi họ kết nối thành công 
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        }

        await base.OnConnectedAsync();
    }
}