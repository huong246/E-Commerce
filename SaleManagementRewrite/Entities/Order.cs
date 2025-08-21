using System.ComponentModel.DataAnnotations.Schema;
using SaleManagementRewrite.Entities.Enum;

namespace SaleManagementRewrite.Entities;
[Table("Order")]
public class Order
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public User? User { get; init; } 
    public decimal TotalAmount {get; set;} // tong tien 
    public decimal TotalShippingFee { get; set; }
    public decimal TotalSubtotal { get; set; }
    public decimal DiscountProductAmount { get; set; }
    public decimal DiscountShippingAmount { get; set; }
    public Address? UserAddress { get; set; } 
    public Guid UserAddressId { get; init; }
    public Guid? VoucherProductId {get; set;}
    public Guid? VoucherShippingId {get; set;}
    public OrderStatus Status { get; set; } = OrderStatus.PendingPayment;
    public DateTime OrderDate { get; init; } = DateTime.UtcNow;
    public ICollection<OrderShop> OrderShops { get; set; } = new List<OrderShop>();
}

