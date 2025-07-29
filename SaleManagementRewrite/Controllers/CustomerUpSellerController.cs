using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SaleManagementRewrite.Entities;
using SaleManagementRewrite.Entities.Enum;
using SaleManagementRewrite.IServices;
using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomerUpSellerController(ICustomerUpSellerService customerUpSellerService) : ControllerBase
{
    [HttpPost("customer_up_seller_request")]
    [Authorize(Roles = nameof(UserRole.Customer))]
    public async Task<IActionResult> CreateCustomerUpSellerRequest()
    {
        var result = await customerUpSellerService.CreateCustomerUpSellerAsync();
        return result switch
        {
            CreateCustomerUpSellerResult.Success => Ok("Request created successfully"),
            CreateCustomerUpSellerResult.TokenInvalid => BadRequest("Token is invalid"),
            CreateCustomerUpSellerResult.NotPermitted => BadRequest("Not permitted"),
            CreateCustomerUpSellerResult.RequestExists => BadRequest("Request exists"),
            CreateCustomerUpSellerResult.UserNotFound => NotFound("User not found"),
            _ => StatusCode(500, "Database Error"),
        };
    }

    [HttpGet("get_request")]
    [Authorize(Roles = nameof(UserRole.Customer))]
    public async Task<CustomerUpSeller?> GetRequest()
    {
        var result = await customerUpSellerService.GetCustomerUpSellerAsync();
        return result;
    }
    
    [HttpPost("approve_request")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<bool> ApproveCustomerUpSellerAsync(ApproveRequest request)
    {
        var result = await customerUpSellerService.ApproveCustomerUpSellerAsync(request);
        return result;
    }

    [HttpPost("reject_request")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<bool> RejectCustomerUpSellerAsync(RejectRequest request)
    {
        var result = await customerUpSellerService.RejectCustomerUpSellerAsync(request);
        return result;
    }
}