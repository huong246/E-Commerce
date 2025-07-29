using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SaleManagementRewrite.Entities;
[Table("Shop")]
public sealed class Shop
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public User? User { get; init; }
    public Guid AddressId { get; init; }
    public required Address Address { get; set; }
    [MaxLength(10000000)]
    public string? Name { get; set; } = string.Empty;
    public int PrepareTime { get; set; }
    public ICollection<Voucher>? Vouchers { get; init; } 
}