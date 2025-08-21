using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SaleManagementRewrite.Entities.Enum;

namespace SaleManagementRewrite.Entities;
[Table("CancelRequest")]
public class CancelRequest
{
    public Guid Id { get; set; }
    public Guid OrderShopId { get; set; }
    public OrderShop? OrderShop { get; set; }
    public RequestStatus Status { get; set; }
    [MaxLength(255)]
    public string Reason { get; set; } = string.Empty;
    public DateTime RequestAt { get; set; }
    public DateTime ReviewAt { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public Guid OrderId { get; set; }
    public Order? Order { get; set; }
    public decimal Amount { get; set; }
}