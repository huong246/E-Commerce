namespace SaleManagementRewrite.Schemas;

public record SendEmailRequest(string Email, string Subject, string Body);