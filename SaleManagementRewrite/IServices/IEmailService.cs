using SaleManagementRewrite.Schemas;

namespace SaleManagementRewrite.IServices;

public interface IEmailService
{
    Task SendEmailAsync(SendEmailRequest request);
}