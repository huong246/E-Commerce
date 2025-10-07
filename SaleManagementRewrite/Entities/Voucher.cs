using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SaleManagementRewrite.Entities.Enum;

namespace SaleManagementRewrite.Entities;
[Table("Voucher")]
public class Voucher
{
    public Guid Id { get; set; }
    [MaxLength(1000)]
    public string Code { get; set; } = string.Empty;
    public Target VoucherTarget{ get; set; }
    public Method VoucherMethod{ get; set; }
    public Guid? ItemId{get;set;}
    public Guid? ShopId{get;set;}
    public Shop? Shop{get;set;}
    public decimal? Maxvalue {get;set;}
    public decimal Value { get; set; }
    public decimal? MinSpend{get;set;}
    public int Quantity {get;set;}
    public DateTime StartDate{get;set;}
    public DateTime EndDate{get;set;}
    public bool IsActive {get;set;}
    public Guid Version { get; set; }
}

 