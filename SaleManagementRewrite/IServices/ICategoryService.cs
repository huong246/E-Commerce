using SaleManagementRewrite.Entities;
using SaleManagementRewrite.Results;
using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.IServices;

public interface ICategoryService
{
    Task<Result<PagedResult<ItemResponse>>> GetItemsAsync(GetItemsRequest request);
    Task<Result<Category>> CreateCategoryAsync(CreateCategoryRequest request);
    Task<Result<Category>> UpdateCategoryAsync(UpdateCategoryRequest request);
    Task<Result<bool>> DeleteCategoryAsync(DeleteCategoryRequest request);
}