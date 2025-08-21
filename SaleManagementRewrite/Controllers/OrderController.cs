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
public class OrderController(IOrderService orderService) : ControllerBase
{
    [HttpPost("create_order")]
    [Authorize(Roles = UserRoles.Customer)]
    public async Task<IActionResult> CreateOrderAsync([FromBody] CreateOrderRequest request)
    {
        var result = await orderService.CreateOrderAsync(request);

        if (!result.IsSuccess)
        {
            return result.ErrorType switch
            {
                ErrorType.NotFound => NotFound(result.Error),
                ErrorType.Unauthorized => Unauthorized(result.Error),
                ErrorType.Conflict => Conflict(result.Error),
                ErrorType.Validation => BadRequest(result.Error),
                ErrorType.Failure => StatusCode(500, result.Error),
                _ => BadRequest(result.Error),
            };
        }

        return CreatedAtAction(nameof(GetOrderById), new { id = result.Value!.OrderId }, result.Value);
    }
    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{UserRoles.Customer}, {UserRoles.Seller}, {UserRoles.Admin}")] 
    public async Task<IActionResult> GetOrderById(Guid id)
    {
        var order = await orderService.GetOrderByIdAsync(id);
        if (order == null)
        {
            return NotFound();
        }
        return Ok(order);
    }
    
    [HttpPost("cancel_order")]
    [Authorize(Roles = UserRoles.Customer)]
    public async Task<IActionResult> CancelMainOrderAsync([FromBody] CancelMainOrderRequest request)
    {
        var result = await orderService.CancelMainOrderAsync(request);
        if (!result.IsSuccess)
        {
            return result.ErrorType switch
            {
                ErrorType.NotFound => NotFound(result.Error),
                ErrorType.Unauthorized => Unauthorized(result.Error),
                ErrorType.Conflict => Conflict(result.Error),
                _ => BadRequest(result.Error),
            };
        }
        return Ok(new { message = "Order has been successfully cancelled." });
    }

    [HttpPost("return_order")]
    [Authorize(Roles =UserRoles.Customer)]
    public async Task<IActionResult> ReturnOrderItemAsync([FromBody] ReturnOrderItemRequest request)
    {
        var result = await orderService.ReturnOrderItemAsync(request);
        if (!result.IsSuccess)
        {
            return result.ErrorType switch
            {
                ErrorType.Conflict => Conflict(result.Error),
                ErrorType.Unauthorized => Unauthorized(result.Error),
                ErrorType.NotFound => NotFound(result.Error),
                _ => BadRequest(result.Error),
            };
        }
        return Accepted(result.Value);
    }

    [HttpPut("approve_return_order")]
    [Authorize(Roles = UserRoles.Seller)]
    public async Task<IActionResult> ApproveReturnOrderAsync([FromBody] ApproveReturnOrderItemRequest request)
    {
        var result =  await orderService.ApproveReturnOrderItemAsync(request);
        if (!result.IsSuccess)
        {
            return result.ErrorType switch
            {
                ErrorType.NotFound => NotFound(result.Error),
                ErrorType.Unauthorized => Unauthorized(result.Error),
                ErrorType.Conflict => Conflict(result.Error),
                _ => BadRequest(result.Error),
            };
        }
        return Ok(new { message = "Approve returnOrder request" });
    }
    [HttpPut("reject_return_order")]
    [Authorize(Roles = UserRoles.Seller)]
    public async Task<IActionResult> RejectReturnOrderAsync([FromBody] RejectReturnOrderItemRequest request)
    {
        var result =  await orderService.RejectReturnOrderItemAsync(request);
        if (!result.IsSuccess)
        {
            return result.ErrorType switch
            {
                ErrorType.NotFound => NotFound(result.Error),
                ErrorType.Unauthorized => Unauthorized(result.Error),
                ErrorType.Conflict => Conflict(result.Error),
                _ => BadRequest(result.Error),
            };
        }
        return Ok(new { message = "Reject returnOrder request" });
    }
    [HttpGet("get_order_history")]
    [Authorize(Roles = $"{UserRoles.Customer}, {UserRoles.Seller}, {UserRoles.Admin}")] 
    public async Task<IActionResult?> GetOrderHistoryAsync(GetOrderHistoryRequest request)
    {
        var result = await orderService.GetOrderHistoryAsync(request);
        if (!result.IsSuccess)
        {
            return result.ErrorType switch
            {
                ErrorType.NotFound => NotFound(result.Error),
                ErrorType.Unauthorized => Unauthorized(result.Error),
                ErrorType.Conflict => Conflict(result.Error),
                _ => BadRequest(result.Error)
            };
        }
        return Ok(result.Value);
    }

    [HttpPut("ship_order_shop")]
    [Authorize(Roles = UserRoles.Seller)]
    public async Task<IActionResult> ShipOrderShopAsync([FromBody] ShipOrderShopRequest request)
    {
        var result = await orderService.ShipOrderShopAsync(request);
        if (!result.IsSuccess)
        {
            return result.ErrorType switch
            {
                ErrorType.NotFound => NotFound(result.Error),
                ErrorType.Unauthorized => Unauthorized(result.Error),
                ErrorType.Conflict => Conflict(result.Error),
                _ => BadRequest(result.Error),
            };
        }
        return Ok(new { message = "OrderShop shipped" });
    }

    [HttpPut("seller_cancel_order")]
    [Authorize(Roles = UserRoles.Seller)]
    public async Task<IActionResult> SellerCancelOrderAsync([FromBody] SellerCancelOrderRequest request)
    {
        var result =  await orderService.SellerCancelOrderAsync(request);
        if (!result.IsSuccess)
        {
            return result.ErrorType switch
            {
                ErrorType.NotFound => NotFound(result.Error),
                ErrorType.Unauthorized => Unauthorized(result.Error),
                ErrorType.Conflict => Conflict(result.Error),
                _ => BadRequest(result.Error),
            };
        }
        return Ok(new { message = "Order cancelled by seller." });
    }

    [HttpPut("mark_shop_order_as_delivered")]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> MarkShopOrderAsDeliveredAsync(MarkShopOrderAsDeliveredRequest request)
    {
        var result = await orderService.MarkShopOrderAsDeliveredAsync(request);
        if (!result.IsSuccess)
        {
            return result.ErrorType switch
            {
                ErrorType.NotFound => NotFound(result.Error),
                ErrorType.Unauthorized => Unauthorized(result.Error),
                ErrorType.Conflict => Conflict(result.Error),
                _ => BadRequest(result.Error),
            };
        }
        return Ok(new { message = "OrderShop delivered." });
    }

    [HttpPut("mark_entire_order_as_completed")]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> MarkOrderShopAsCompletedAsync(MarkOrderShopAsCompletedRequest request)
    {
        var result = await orderService.MarkOrderShopAsCompletedAsync(request);
        if (!result.IsSuccess)
        {
            return result.ErrorType switch
            {
                ErrorType.NotFound => NotFound(result.Error),
                ErrorType.Unauthorized => Unauthorized(result.Error),
                ErrorType.Conflict => Conflict(result.Error),
                _ => BadRequest(result.Error),
            };
        }
        return Ok(new { message = "OrderShop completed." });
    }

    [HttpPut("cancel_entire_order")]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> CancelEntireOrderAsync(CancelEntireOrderRequest request)
    {
        var result = await orderService.CancelEntireOrderAsync(request);
        if (!result.IsSuccess)
        {
            return result.ErrorType switch
            {
                ErrorType.NotFound => NotFound(result.Error),
                ErrorType.Unauthorized => Unauthorized(result.Error),
                ErrorType.Conflict => Conflict(result.Error),
                _ => BadRequest(result.Error),
            };
        }
        return Ok(new { message = "Order cancelled by admin." });
    }

    [HttpPut("process_return_request")]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> ProcessReturnRequestAsync(ProcessReturnRequestRequest request)
    {
        var result =  await orderService.ProcessReturnRequestAsync(request);
        if (!result.IsSuccess)
        {
            return result.ErrorType switch
            {
                ErrorType.NotFound => NotFound(result.Error),
                ErrorType.Unauthorized => Unauthorized(result.Error),
                ErrorType.Conflict => Conflict(result.Error),
                _ => BadRequest(result.Error),
            };
        }
        return Ok(new { message = "Return successfully." });
    }
    
}