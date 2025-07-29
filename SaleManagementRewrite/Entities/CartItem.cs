using System.ComponentModel.DataAnnotations.Schema;

namespace SaleManagementRewrite.Entities;
[Table("CartItem")]
public sealed class CartItem
{
    public Guid Id { get; init; }
    public Guid ItemId { get; init; }
    public Item? Item { get; init; }
    public Guid ShopId { get; init; }
    public Shop? Shop { get; init; }
    public Guid UserId { get; init; }
    public User? User { get; init; }
    public int Quantity { get; set; }
    
}