using System.ComponentModel.DataAnnotations.Schema;

namespace SaleManagementRewrite.Entities;
[Table("Conversation")]
public class Conversation
{
    public Guid Id { get; init; }
    public Guid ParticipantAId { get; init; }
    public Guid ParticipantBId { get; init; }
    public ICollection<Message> Messages { get; init; } = new List<Message>();
}