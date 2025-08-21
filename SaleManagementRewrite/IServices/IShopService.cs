using SaleManagementRewrite.Entities;
using SaleManagementRewrite.Results;
using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.IServices;
 
public interface IShopService
{
    Task<Result<Shop>> CreateShop(CreateShopRequest request);
    Task<Shop?>  GetShopByIdAsync(Guid id);
    Task<Result<Shop>> UpdateShop(UpdateShopRequest request);
}