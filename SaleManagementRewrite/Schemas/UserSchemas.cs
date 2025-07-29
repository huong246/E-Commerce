namespace SaleManagementRewrite.Schemas;

public record RegisterRequest(string Username, string Password, string PhoneNumber, string FullName);
public record LoginRequest(string Username, string Password);