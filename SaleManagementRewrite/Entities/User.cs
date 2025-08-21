using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using SaleManagementRewrite.Entities.Enum;

namespace SaleManagementRewrite.Entities;

[Table("Users")]
public class User : IdentityUser<Guid>
{
    //public Guid Id { get; init; }
    [MaxLength(1000)]
    public string? FullName { get; set; } = string.Empty;
    public DateTime? Birthday { get; set; }
    [MaxLength(1000)]
    public string? Gender { get; set; } = string.Empty;
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Balance { get; set; } 
    public List<Address> Addresses { get; set; } = [];
    [MaxLength(500)]
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    [MaxLength(50)]
    public string UserRole { get; set; } =  string.Empty;
}