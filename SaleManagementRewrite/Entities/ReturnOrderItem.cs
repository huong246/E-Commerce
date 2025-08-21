using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SaleManagementRewrite.Entities.Enum;

namespace SaleManagementRewrite.Entities;

[Table("ReturnOrderItem")]
public sealed class ReturnOrderItem
{
    public Guid Id { get; set; }
    public Guid? ReturnOrderId { get; set; }
    public required ReturnOrder ReturnOrder { get; set; }
    public Guid OrderItemId { get; set; }
    public required OrderItem OrderItem{ get; set; }
    public ReturnStatus Status { get; set; }

    [MaxLength(10000)] 
    public string? Reason { get; set; }
    public int Quantity { get; init; }
    [MaxLength(10000)]
    public string? ReturnShippingTrackingCode { get; init; }= string.Empty;
    public decimal Amount { get; init; }

}