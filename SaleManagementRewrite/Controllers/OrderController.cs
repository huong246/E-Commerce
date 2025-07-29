using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaleManagementRewrite.Entities.Enum;
using SaleManagementRewrite.IServices;
using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrderController(IOrderService orderService) : ControllerBase
{
    [HttpPost("create_order")]
    [Authorize(Roles = nameof(UserRole.Customer))]
    public async Task<IActionResult> CreateOrderAsync([FromBody] CreateOrderRequest request)
    {
        var result = await orderService.CreateOrderAsync(request);
        return result switch
        {
            CreateOrderResult.Success => Ok("Order created successfully"),
            CreateOrderResult.ConcurrencyConflict => Conflict("Concurrency conflict"),
            CreateOrderResult.AddressNotFound => NotFound("Address not found"),
            CreateOrderResult.TokenInvalid => BadRequest("Token invalid"),
            CreateOrderResult.CartItemIsEmpty => BadRequest("Cart item is empty"),
            CreateOrderResult.CartItemNotFound => NotFound("Cart item not found"),
            CreateOrderResult.InsufficientStock => Forbid("Insufficient stock"),
            CreateOrderResult.MinSpendNotMet => Forbid("Min Spend not met"),
            CreateOrderResult.VoucherExpired => Forbid("Voucher expired"),
            CreateOrderResult.UserNotFound => Forbid("User not found"),
            CreateOrderResult.OutOfStock  => Forbid("Out of stock"),
            _ => StatusCode(500, "Database Error"),
        };
    }

    [HttpPost("cancel_order")]
    [Authorize(Roles = nameof(UserRole.Customer))]
    public async Task<IActionResult> CancelMainOrderAsync([FromBody] CancelMainOrderRequest request)
    {
        var result = await orderService.CancelMainOrderAsync(request);
        return result switch
        {
            CancelMainOrderResult.Success => Ok("Order cancelled successfully"),
            CancelMainOrderResult.TokenInvalid => BadRequest("Token invalid"),
            CancelMainOrderResult.OrderNotFound => NotFound("Order not found"),
            CancelMainOrderResult.UserNotFound => NotFound("User not found"),
            CancelMainOrderResult.NotPermitted => Forbid("Not permitted"),
            CancelMainOrderResult.ConcurrencyConflict  => Conflict("Concurrency conflict"),
            _ => StatusCode(500, "Database Error"),
        };
    }

    [HttpPost("return_order")]
    [Authorize(Roles = nameof(UserRole.Customer))]
    public async Task<IActionResult> ReturnOrderItemAsync([FromBody] ReturnOrderItemRequest request)
    {
        var result = await orderService.ReturnOrderItemAsync(request);
        return result switch
        {
            ReturnOrderItemResult.Success => Ok("Return order request created successfully"),
            ReturnOrderItemResult.TokenInvalid => BadRequest("Token invalid"),
            ReturnOrderItemResult.OrderNotFound => NotFound("Order not found"),
            ReturnOrderItemResult.UserNotFound => NotFound("User not found"),
            ReturnOrderItemResult.NotPermitted => Forbid("Not permitted"),
            ReturnOrderItemResult.ReturnPeriodExpired  => Forbid("Return period expired"),
            ReturnOrderItemResult.QuantityReturnInvalid => BadRequest("Quantity return invalid"),
            ReturnOrderItemResult.ConcurrencyConflict => Conflict("Concurrency conflict"),
            _ => StatusCode(500, "Database Error"),
        };
    }

    [HttpPost("approve_return_order")]
    [Authorize(Roles = nameof(UserRole.Seller))]
    public async Task<IActionResult> ApproveReturnOrderAsync([FromBody] ApproveReturnOrderItemRequest request)
    {
        var result =  await orderService.ApproveReturnOrderItemAsync(request);
        return Ok(result);
    }
    [HttpPost("reject_return_order")]
    [Authorize(Roles = nameof(UserRole.Seller))]
    public async Task<IActionResult> RejectReturnOrderAsync([FromBody] RejectReturnOrderItemRequest request)
    {
        var result =  await orderService.RejectReturnOrderItemAsync(request);
        return Ok(result);
    }
    [HttpGet("get_order_history")]
    [Authorize(Roles = $"{nameof(UserRole.Customer)}, {nameof(UserRole.Seller)}, {nameof(UserRole.Admin)}")] 
    public async Task<IActionResult?> GetOrderHistoryAsync(GetOrderHistoryRequest request)
    {
        var result = await orderService.GetOrderHistoryAsync(request);
        return !result.Any() ? null : Ok(result);
    }

    [HttpPost("ship_order_shop")]
    [Authorize(Roles = nameof(UserRole.Seller))]
    public async Task<IActionResult> ShipOrderShopAsync([FromBody] ShipOrderShopRequest request)
    {
        var result = await orderService.ShipOrderShopAsync(request);
        return result switch
        {
            ShipOrderShopResult.Success => Ok("OrderShop shipped"),
            ShipOrderShopResult.TokenInvalid => BadRequest("Token invalid"),
            ShipOrderShopResult.OrderNotFound => NotFound("Order not found"),
            ShipOrderShopResult.UserNotFound => NotFound("User not found"),
            ShipOrderShopResult.ConcurrencyConflict => Conflict("Concurrency conflict"),
            ShipOrderShopResult.ShopNotFound => NotFound("Shop not found"),
            ShipOrderShopResult.NotPermitted => Forbid("Not permitted"),
            _ => StatusCode(500, "Database Error"),
        };
    }

    [HttpPost("seller_cancel_order")]
    [Authorize(Roles = nameof(UserRole.Seller))]
    public async Task<IActionResult> SellerCancelOrderAsync([FromBody] SellerCancelOrderRequest request)
    {
        var result =  await orderService.SellerCancelOrderAsync(request);
        return result switch
        {
            SellerCancelOrderResult.Success => Ok("Order cancelled"),
            SellerCancelOrderResult.TokenInvalid => BadRequest("Token invalid"),
            SellerCancelOrderResult.OrderNotFound => NotFound("Order not found"),
            SellerCancelOrderResult.UserNotFound => NotFound("User not found"),
            SellerCancelOrderResult.ConcurrencyConflict => Conflict("Concurrency conflict"),
            SellerCancelOrderResult.ShopNotFound => NotFound("Shop not found"),
            SellerCancelOrderResult.NotPermitted => Forbid("Not permitted"),
            SellerCancelOrderResult.CustomerNotFound => NotFound("Customer not found"),
            _ => StatusCode(500, "Database Error"),
        };
    }

    [HttpPost("mark_shop_order_as_delivered")]
    public async Task<IActionResult> MarkShopOrderAsDeliveredAsync(MarkShopOrderAsDeliveredRequest request)
    {
        var result = await orderService.MarkShopOrderAsDeliveredAsync(request);
        return result switch
        {
            MarkShopOrderAsDeliveredResult.Success => Ok("Order marked delivered"),
            MarkShopOrderAsDeliveredResult.TokenInvalid => BadRequest("Token invalid"),
            MarkShopOrderAsDeliveredResult.OrderNotFound => NotFound("Order not found"),
            MarkShopOrderAsDeliveredResult.UserNotFound => NotFound("User not found"),
            MarkShopOrderAsDeliveredResult.ConcurrencyConflict => Conflict("Concurrency conflict"),
            MarkShopOrderAsDeliveredResult.NotPermitted => Forbid("Not permitted"),
            _=> StatusCode(500, "Database Error"),
        };
    }

    [HttpPost("mark_entire_order_as_completed")]
    public async Task<IActionResult> MarkEntireOrderAsCompletedAsync(MarkEntireOrderAsCompletedRequest request)
    {
        var result = await orderService.MarkEntireOrderAsCompletedAsync(request);
        return result switch
        {
            MarkEntireOrderAsCompletedResult.Success => Ok("Order marked completed"),
            MarkEntireOrderAsCompletedResult.TokenInvalid => BadRequest("Token invalid"),
            MarkEntireOrderAsCompletedResult.OrderNotFound => NotFound("Order not found"),
            MarkEntireOrderAsCompletedResult.UserNotFound => NotFound("User not found"),
            MarkEntireOrderAsCompletedResult.ReturnPeriodNotExpired => Forbid("Not permitted"),
            MarkEntireOrderAsCompletedResult.ConcurrencyConflict => Conflict("Concurrency conflict"),
            MarkEntireOrderAsCompletedResult.NotPermitted => Forbid("Not permitted"),
            _ => StatusCode(500, "Database Error"),
        };
    }

    [HttpPost("cancel_entire_order")]
    public async Task<IActionResult> CancelEntireOrderAsync(CancelEntireOrderRequest request)
    {
        var result = await orderService.CancelEntireOrderAsync(request);
        return result switch
        {
            CancelEntireOrderResult.Success => Ok("Order cancelled"),
            CancelEntireOrderResult.TokenInvalid => BadRequest("Token invalid"),
            CancelEntireOrderResult.OrderNotFound => NotFound("Order not found"),
            CancelEntireOrderResult.UserNotFound => NotFound("User not found"),
            CancelEntireOrderResult.ConcurrencyConflict => Conflict("Concurrency conflict"),
            CancelEntireOrderResult.NotPermitted => Forbid("Not permitted"),
            _ => StatusCode(500, "Database Error"),
        };
    }

    [HttpPost("process_return_request")]
    public async Task<IActionResult> ProcessReturnRequestAsync(ProcessReturnRequestRequest request)
    {
        var result =  await orderService.ProcessReturnRequestAsync(request);
        return result switch
        {
            ProcessReturnRequestResult.TokenInvalid => BadRequest("Token invalid"),
            ProcessReturnRequestResult.AlreadyProcessed => Forbid("Already processed"),
            ProcessReturnRequestResult.UserNotFound => NotFound("User not found"),
            ProcessReturnRequestResult.ReturnOrderNotFound => NotFound("Order not found"),
            ProcessReturnRequestResult.ReasonIsRequiredForRejection => Forbid("Rejection"),
            ProcessReturnRequestResult.Success => Ok("Order processed"),
            ProcessReturnRequestResult.NotPermitted => Forbid("Not permitted"),
            _ => StatusCode(500, "Database Error"),
        };
    }
    
}