using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SaleManagementRewrite.Entities;
[Table("Message")]
public sealed class Message
{
    public Guid Id { get; init; }
    public Guid ConversationId { get; init; }
    public Conversation? Conversation { get; init; }
    public Guid SenderId { get; init; } 
    public User? Sender { get; init; }
    public Guid RecipientId { get; init; }
    public User? Recipient { get; init; }
    [MaxLength(1000000000)]
    public string Context { get; init; } = string.Empty;
    public DateTime SendAt { get; init; } = DateTime.UtcNow;
    public bool IsRead { get; init; } 
    
}