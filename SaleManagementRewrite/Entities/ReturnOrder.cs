using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SaleManagementRewrite.Entities.Enum;

namespace SaleManagementRewrite.Entities;
[Table("ReturnOrder")]
public sealed class ReturnOrder
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public ReturnStatus Status { get; set; }
    public DateTime RequestAt { get; set; }
    public DateTime? ReviewAt { get; set; }
    public ICollection<ReturnOrderItem> ReturnOrderItems { get; init; } = new List<ReturnOrderItem>();
    public decimal Amount { get; set; }
}