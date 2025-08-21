using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaleManagementRewrite.IServices;
using SaleManagementRewrite.Results;
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
        return HandleResult(result);
    }

    [HttpPost("create_payment_order")]
    public async Task<IActionResult> CreatePaymentAsync([FromBody]CreatePaymentRequest request)
    {
        var result = await transactionService.CreatePaymentAsync(request);
        return HandleResult(result);
    }

    [HttpGet("get_transaction")]
    public async Task<IActionResult> GetTransactionAsync(GetTransactionForOrderRequest request)
    {
        var result = await transactionService.GetTransactionAsync(request);
        return HandleResult(result);
    }
    [HttpGet("vnPay-ipn")]
    public async Task<IActionResult> HandleVnPayIpn()
    {
        var vnPayData = HttpContext.Request.Query;
        var result = await transactionService.ProcessIpnAsync(vnPayData);
        return Ok(result);
    }
    [HttpPost("vnPay")]
    public async Task<IActionResult> CreateVnPayPayment([FromBody] CreatePaymentVnPayRequest request)
    {
        try
        {
            request = request with { IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty };
            var paymentUrl = await transactionService.CreatePaymentVnPayUrlAsync(request);
            return Ok(new { payUrl = paymentUrl });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
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