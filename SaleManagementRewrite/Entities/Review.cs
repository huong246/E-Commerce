using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SaleManagementRewrite.Entities;
[Table("Review")]
public sealed class Review
{
    public Guid Id { get; init; }
    public Guid OrderId { get; init; }
    public Order? Order { get; init; }
    public Guid ItemId { get; init; }
    public Item? Item { get; init; }
    public Guid UserId { get; init; }
    public User? User { get; init; }
    public int Rating { get; init; }
    [MaxLength(100000000)]
    public string Comment { get; init; } = string.Empty;
    public DateTime ReviewAt { get; init; }
    
}