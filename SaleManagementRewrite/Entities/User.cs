using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using SaleManagementRewrite.Entities.Enum;

namespace SaleManagementRewrite.Entities;

[Table("Users")]
public class User
{
    public Guid Id { get; init; }
    [MaxLength(100000000)]
    public string? FullName { get; set; } = string.Empty;
    [MaxLength(100000000)]
    public string? Email { get; set; } = string.Empty;
    [MaxLength(100000000)]
    public string? PhoneNumber { get; set; } = string.Empty;
    [MaxLength(100000000)]
    public string Username { get; init; } = string.Empty;
    [MaxLength(100000000)]
    [MinLength(8)]
    public string? Password { get; set; }  = string.Empty;
    public DateTime? Birthday { get; set; }
    [MaxLength(100000000)]
    public string? Gender { get; set; } = string.Empty;
    public UserRole UserRole { get; set; }
    public decimal Balance { get; set; } 
    public List<Address> Addresses { get; init; } = [];
    [MaxLength(1000000)]
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
}