using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SaleManagementRewrite.Entities;
[Table("Address")]
public class Address
{
    public Guid Id { get; set; }
    [MaxLength(100)]  
    public string? Name { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public User? User { get; set; } 
    public double Longitude { get; set; }
    public double Latitude { get; set; }
    public bool? IsDefault { get; set; }
}