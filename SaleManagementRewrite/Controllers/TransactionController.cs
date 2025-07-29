using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaleManagementRewrite.Entities;
using SaleManagementRewrite.IServices;
using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionController(ITransactionService transactionService) : ControllerBase
{
    [HttpPost("deposit_money_into_balance")]
    public async Task<IActionResult> DepositIntoBalanceASync([FromBody]DepositIntoBalanceRequest request)
    {
        var result = await transactionService.DepositIntoBalanceAsync(request);
        return result switch
        {
            DepositIntoBalanceResult.Success => Ok("Deposit money into balance"),
            DepositIntoBalanceResult.AmountInvalid => BadRequest("Amount is invalid"),
            DepositIntoBalanceResult.TokenInvalid => BadRequest("Token is invalid"),
            DepositIntoBalanceResult.UserNotFound => NotFound("User not found"),
            DepositIntoBalanceResult.ConcurrencyConflict => Conflict("Concurrency"),
            _ => StatusCode(500, "Database Error"),
        };
    }

    [HttpPost("create_payment_order")]
    public async Task<IActionResult> CreatePaymentAsync([FromBody]CreatePaymentRequest request)
    {
        var result = await transactionService.CreatePaymentAsync(request);
        return result switch
        {
            CreatePaymentResult.Success => Ok("Payment created"),
            CreatePaymentResult.UserNotFound => NotFound("User not found"),
            CreatePaymentResult.ConcurrencyConflict => Conflict("Concurrency"),
            CreatePaymentResult.AmountInvalid => BadRequest("Amount is invalid"),
            CreatePaymentResult.NotPermitted => BadRequest("NotPermitted"),
            CreatePaymentResult.PlatformWalletNotFound => NotFound("Platform wallet not found"),
            CreatePaymentResult.AmountMismatch => BadRequest("Amount is mismatch"),
            CreatePaymentResult.InvalidState => BadRequest("Invalid state"),
            CreatePaymentResult.OrderNotFound => NotFound("Order not found"),
            CreatePaymentResult.BalanceNotEnough => BadRequest("Balance not enough"),
            _ => StatusCode(500, "Database Error"),
        };
    }

    [HttpPost("create_payout_seller")]
    public async Task<IActionResult> CreatePayOutAsync([FromBody] CreatePayOutRequest request)
    {
        var result = await transactionService.CreatePayOutAsync(request);
        return result switch
        {
            CreatePayOutResult.Success => Ok("Payout created"),
            CreatePayOutResult.UserNotFound => NotFound("User not found"),
            CreatePayOutResult.ConcurrencyConflict => Conflict("Concurrency"),
            CreatePayOutResult.AmountMismatch => BadRequest("Amount is mismatch"),
            CreatePayOutResult.InvalidState => BadRequest("Invalid state"),
            CreatePayOutResult.OrderNotFound => NotFound("Order not found"),
            CreatePayOutResult.AmountInvalid => BadRequest("Amount is invalid"),
            CreatePayOutResult.OrderNotOfShop => NotFound("Order not of a shop"),
            CreatePayOutResult.PlatformWalletNotFound => NotFound("Platform wallet not found"),
            CreatePayOutResult.ShopNotFound => NotFound("Shop not found"),
            _ => StatusCode(500, "Database Error"),
        };
    }

    [HttpPost("create_refund")]
    public async Task<IActionResult> CreateRefundAsync([FromBody] CreateRefundRequest request)
    {
        var result = await transactionService.CreateRefundAsync(request);
        return result switch
        {
            CreateRefundResult.Success => Ok("Refund created"),
            CreateRefundResult.UserNotFound => NotFound("User not found"),
            CreateRefundResult.ConcurrencyConflict => Conflict("Concurrency"),
            CreateRefundResult.AmountMismatch => BadRequest("Amount is mismatch"),
            CreateRefundResult.InvalidState => BadRequest("Invalid state"),
            CreateRefundResult.PlatformWalletNotFound => NotFound("Platform wallet not found"),
            CreateRefundResult.AmountInvalid => BadRequest("Amount is invalid"),
            CreateRefundResult.ReturnOrderNotFound => NotFound("Return order not found"),
            _ => StatusCode(500, "Database Error"),
        };
    }

    [HttpGet("get_transaction")]
    public async Task<IEnumerable<Transaction?>> GetTransactionAsync(GetTransactionForOrderRequest request)
    {
        return  await transactionService.GetTransactionAsync(request);
    }
}