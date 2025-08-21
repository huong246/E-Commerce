using Microsoft.AspNetCore.Identity;
using SaleManagementRewrite.Entities;
using SaleManagementRewrite.IServices;
using SaleManagementRewrite.Schemas;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace SaleManagementRewrite.Services;

public class EmailService(IConfiguration configuration, UserManager<User> userManager, ILogger<EmailService> logger) : IEmailService
{
    private readonly string? _apiKey = configuration["SendGrid:ApiKey"];
    private readonly string? _fromEmail = configuration["SendGrid:FromEmail"];
    private readonly string? _fromName = configuration["SendGrid:FromName"];
    public async Task SendEmailAsync(SendEmailRequest request)
    {
        if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_fromEmail))
        {
            logger.LogError("SendGrid API Key or FromEmail is not configured in appSettings.json.");
            return;
        }

        try
        {
            var client = new SendGridClient(_apiKey);
            var from = new EmailAddress(_fromEmail, _fromName);
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return;
            }
            var to = new EmailAddress(request.Email, user.FullName); 
            var msg = MailHelper.CreateSingleEmail(from, to, request.Subject, "", request.Body);
            var response = await client.SendEmailAsync(msg);
            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Email sent successfully to {EmailAddress}", request.Email);
            }
            else
            {
                var errorBody = await response.Body.ReadAsStringAsync();
                logger.LogError(
                    "Email sending failed. Error code from SendGrid: {StatusCode}. Detail: {ErrorBody}", 
                    response.StatusCode, 
                    errorBody);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unexpected error occurred while sending email.");
        }
    }
}