using Microsoft.AspNetCore.SignalR;

namespace SaleManagementRewrite.Hubs;

public class NotificationHubs : Hub
{
    public async Task SendNotificationToUser(Guid userId, string message)
    {
       await Clients.Users(userId.ToString()).SendAsync("ReceiveMessage", message);
    }

    public async Task SendNotificationToGroup(Guid groupId, string message)
    {
        await Clients.Group(groupId.ToString()).SendAsync("ReceiveMessage", message);
    }
    
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