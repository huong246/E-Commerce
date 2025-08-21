
using SaleManagementRewrite.Entities;

namespace SaleManagementRewrite.Schemas
{
    public record CreateShopRequest(string Name, double Latitude, double Longitude, string NameAddress, int PrepareTime);
  
    public record UpdateShopRequest(Guid Id, string? Name, Guid? AddressId, double? Latitude, double? Longitude, string? NameAddress, int? PrepareTime);
}

