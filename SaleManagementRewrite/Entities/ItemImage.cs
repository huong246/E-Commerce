using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SaleManagementRewrite.Entities;
[Table("ItemImage")]
public class ItemImage
{
    public Guid Id { get; init; }
    public Guid ItemId { get; init; }
    public Item? Item { get; init; }
    [MaxLength(100)]  
    public string ImageUrl { get; init; } =  string.Empty;
    public bool IsAvatar { get; set; }
}