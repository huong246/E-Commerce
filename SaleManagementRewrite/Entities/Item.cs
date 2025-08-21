using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SaleManagementRewrite.Entities;

[Table("Item")]
public class Item
{
    public Guid Id { get; init; }
    [MaxLength(100)]  
    public string? Name { get; set; } = string.Empty;
    public Guid ShopId { get; init; }
    public required Shop Shop { get; init; } 
    public decimal Price { get; set; }
    public int? Stock { get; set; }
    [MaxLength(25000)]  
    public string? Description { get; set; } = string.Empty;
    [MaxLength(100)]  
    public string? Color { get; set; } = string.Empty;
    [MaxLength(100)]  
    public string? Size { get; set; } = string.Empty;
    public int SaleCount { get; init; }
    public ICollection<ItemImage> ItemImages { get; init; } = new HashSet<ItemImage>();
    public Guid CategoryId { get; set; }
    public Category? Category { get; set; } 
    public virtual ICollection<Review>? Reviews { get; set; }
    public Guid Version { get; set; }
}