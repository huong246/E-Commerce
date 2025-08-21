namespace SaleManagementRewrite.Schemas;

public record UpdateUserProfileRequest(string? Fullname, string? Email, string? PhoneNumber, DateTime? Birthday, string? Gender); 
public record UpdatePasswordRequest(string? OldPassword, string? NewPassword);