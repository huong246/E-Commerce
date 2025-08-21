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
public class CustomerUpSellerController(ICustomerUpSellerService customerUpSellerService) : ControllerBase
{
    [HttpPost("customer_up_seller_request")]
    [Authorize(Roles = UserRoles.Customer)]
    public async Task<IActionResult> CreateCustomerUpSellerRequest()
    {
        var result = await customerUpSellerService.CreateCustomerUpSellerAsync();
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

    [HttpGet("get_request")]
    [Authorize(Roles = UserRoles.Customer)]
    public async Task<IActionResult> GetRequest()
    {
        var result = await customerUpSellerService.GetCustomerUpSellerAsync();
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
    
    [HttpPost("approve_request")]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> ApproveCustomerUpSellerAsync(ApproveRequest request)
    {
        var result = await customerUpSellerService.ApproveCustomerUpSellerAsync(request);
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

    [HttpPost("reject_request")]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<IActionResult> RejectCustomerUpSellerAsync(RejectRequest request)
    {
        var result = await customerUpSellerService.RejectCustomerUpSellerAsync(request);
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
}