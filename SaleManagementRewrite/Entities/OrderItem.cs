using System.ComponentModel.DataAnnotations.Schema;
using SaleManagementRewrite.Entities.Enum;

namespace SaleManagementRewrite.Entities;
[Table("OrderItem")]
public sealed class OrderItem
{
    public Guid Id { get; init; }
    public Guid? OrderShopId { get; init; }
    public required OrderShop? OrderShop { get; init; }
    public Guid ItemId { get; init; }
    public Item? Item { get; init; }
    public int Quantity { get; init; }
    public decimal Price { get; init; }
    public decimal TotalAmount { get; init; }
    public Guid ShopId { get; init; }
    public Shop? Shop { get; init; }
    public OrderItemStatus Status { get; set; }
}