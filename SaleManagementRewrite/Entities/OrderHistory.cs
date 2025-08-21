using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SaleManagementRewrite.Entities.Enum;

namespace SaleManagementRewrite.Entities;
[Table("OrderHistory")]
public class OrderHistory
{
    public Guid Id { get; init; }
    public DateTime CreateAt { get; init; } = DateTime.UtcNow;
    [MaxLength(100)]
    public string? FromStatus { get; init; }
    [MaxLength(100)]
    public string? ToStatus { get; init; }
    public Guid? ChangedByUserId { get; init; } 
    [MaxLength(10000)]
    public string? Note  { get; init; }
    public Guid OrderId { get; init; }
    public virtual Order? Order { get; init; }
    public Guid? OrderShopId { get; init; } 
    public Guid? OrderItemId { get; init; }
}