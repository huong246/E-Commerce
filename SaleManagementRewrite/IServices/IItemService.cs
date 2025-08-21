using SaleManagementRewrite.Entities;
using SaleManagementRewrite.Results;
using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.IServices;
 
public interface IItemService
{
    Task<Result<CreateItemResponse>> CreateItemAsync(CreateItemRequest request);
    
   
    Task<Result<Item>> UpdateItemAsync(UpdateItemRequest request);
    Task<Result<bool>> DeleteItemAsync(DeleteItemRequest request);
    
}