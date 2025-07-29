using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SaleManagementRewrite.Entities.Enum;

namespace SaleManagementRewrite.Entities;

[Table("OrderShop")]
public class OrderShop
{
    public Guid Id { get; init; }
    public Guid OrderId { get; init; }
    public Order? Order { get; init; }
    public ICollection<OrderItem> OrderItems { get; init; } =  new List<OrderItem>();
    public decimal DiscountShopAmount { get; set; }
    public Guid? VoucherShopId {get; set;}
    public Voucher? VoucherShop {get; set;}
    [MaxLength(10000000)]
    public string? VoucherShopCode {get; set;}
    public decimal TotalShopAmount {get; init;} // tong tien sau voucher
    public OrderShopStatus Status { get; set; }
    [MaxLength(1000000)]
    public string? Notes { get; set; }
    public Guid ShopId { get; init; }
    public Shop? Shop { get; init; }
    public decimal SubTotalShop {get; set;}
    public decimal ShippingFee {get; set;}
    public DateTime? DeliveredDate { get; set; }
    [MaxLength(1000000)]
    public string? TrackingCode {get; init;}
    
}