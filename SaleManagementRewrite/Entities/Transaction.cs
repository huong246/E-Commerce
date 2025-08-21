using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SaleManagementRewrite.Entities.Enum;

namespace SaleManagementRewrite.Entities;

[Table(("Transaction"))]
public class Transaction
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public TransactionStatus Status { get; set; }
    public Guid? FromUserId { get; set; }
    public Guid? ToUserId { get; set; }
    public Guid? OrderId { get; set; }
    public Order? Order { get; set; }
    public DateTime CreateAt { get; set; }
    [MaxLength(10000)]
    public string? Notes { get; set; }
    public Guid? OrderShopId {get; set; }
    public OrderShop? OrderShop { get; set; }
    public Guid? ReturnOrderId { get; set; }
    public ReturnOrder? ReturnOrder { get; set; }
    public Guid? CancelRequestId { get; set; }
    public CancelRequest? CancelRequest { get; set; }
}