using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaleManagementRewrite.Entities;
using SaleManagementRewrite.Entities.Enum;
using SaleManagementRewrite.IServices;
using SaleManagementRewrite.Results;
using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.Controllers;
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoryController(ICategoryService categoryService) : ControllerBase
{
    [HttpGet("get-items")]
    [AllowAnonymous] 
    public async Task<IActionResult> GetItems([FromQuery] GetItemsRequest request)
    {
        var result = await categoryService.GetItemsAsync(request);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPost("create-category")]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        var result = await categoryService.CreateCategoryAsync(request);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpPut("update-category")]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> UpdateCategory([FromBody] UpdateCategoryRequest request)
    {
        var result = await categoryService.UpdateCategoryAsync(request);
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }

    [HttpDelete("delete-category")]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> DeleteCategory([FromBody] DeleteCategoryRequest request)
    {
        var result = await categoryService.DeleteCategoryAsync(request);
        return result.IsSuccess ? Ok() : HandleFailure(result);
    }
    
    [HttpGet("get-all-categories")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllCategories()
    {
        var result = await categoryService.GetAllCategoriesAsync();
        return result.IsSuccess ? Ok(result.Value) : HandleFailure(result);
    }
    private IActionResult HandleFailure<T>(Result<T> result)
    {
        return result.ErrorType switch
        {
            ErrorType.Validation => BadRequest(result.Error),
            ErrorType.NotFound => NotFound(result.Error),
            ErrorType.Conflict => Conflict(result.Error),
            ErrorType.Unauthorized => Unauthorized(result.Error),
            _ => StatusCode(500, result.Error)  
        };
    }
    
}