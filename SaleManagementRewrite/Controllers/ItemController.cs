using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaleManagementRewrite.Data;
using SaleManagementRewrite.Entities;
using SaleManagementRewrite.Entities.Enum;
using SaleManagementRewrite.IServices;
using SaleManagementRewrite.Results;
using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ItemController(IItemService itemService, ApiDbContext dbContext, ILogger<ItemController> logger) : ControllerBase
{

    [HttpPost("create_item")]
    [Authorize(Roles = UserRoles.Seller)]
    public async Task<IActionResult> CreateItem([FromBody] CreateItemRequest request)
    {
        var result = await itemService.CreateItemAsync(request);
        return HandleResult(result);
    }

    [HttpPost("update_item")]
    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Seller}")] 
    public async Task<IActionResult> UpdateItem([FromBody] UpdateItemRequest request)
    {
        var result = await itemService.UpdateItemAsync(request);
        return HandleResult(result);
    }
    
    [HttpDelete("delete_item")]
    [Authorize(Roles = $"{UserRoles.Admin},{UserRoles.Seller}")]
    public async Task<IActionResult> DeleteItem([FromBody] DeleteItemRequest request)
    {
        var result = await itemService.DeleteItemAsync(request);
        return HandleResult(result);
    }
    [HttpGet("list_items_random")] 
    [AllowAnonymous]
    public async Task<IActionResult> GetRandomItems([FromQuery] int count = 10)
    {
        if (count <= 0)
        {
            return BadRequest("count <= 0.");
        }
        try
        {
            var randomItems = await dbContext.Items
                    .FromSqlRaw("SELECT * FROM Items ORDER BY RANDOM() LIMIT {0}", count)
                    .ToListAsync();

            return Ok(randomItems);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi xảy ra khi đang lấy item ngẫu nhiên.");
            return StatusCode(500, "Database error.");
        }
    }
    private IActionResult HandleResult<T>(Result<T> result)
    {
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