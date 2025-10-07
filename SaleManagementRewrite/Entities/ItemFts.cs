using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace SaleManagementRewrite.Entities;
[Keyless]
public class ItemFts
{
    public long rowid { get; set; } 
    [MaxLength(100)]
    public string? Name { get; set; }
    [MaxLength(100)]
    public string? Description { get; set; }
    [MaxLength(100)]
    public string? Color { get; set; }
    [MaxLength(100)]
    public string? Size { get; set; }
    
 
}