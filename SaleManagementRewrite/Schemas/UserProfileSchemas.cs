namespace SaleManagementRewrite.Schemas;

public record UpdateUserProfileRequest(string? Fullname, string? Email, string? PhoneNumber, DateTime? Birthday, string? Gender); 
public record UpdatePasswordRequest(string? OldPassword, string? NewPassword);
public record UserProfileDto(Guid Id, string Username,  string? Fullname, string? PhoneNumber, DateTime? Birthday, string? Gender );