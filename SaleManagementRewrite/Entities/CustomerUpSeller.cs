using System.ComponentModel.DataAnnotations.Schema;
using SaleManagementRewrite.Entities.Enum;

namespace SaleManagementRewrite.Entities;
[Table(("CustomerUpSeller"))]
public sealed class CustomerUpSeller
{
    public Guid Id {get; init;}
    public Guid UserId {get; init;}
    public User? User {get; init;}
    public RequestStatus Status {get; set;} = RequestStatus.Pending;
    public DateTime RequestAt {get; init;} = DateTime.UtcNow;
    public DateTime? ReviewAt {get; set;}
}