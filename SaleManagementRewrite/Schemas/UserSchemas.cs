namespace SaleManagementRewrite.Schemas;

public record RegisterRequest(string Username, string Password, string PhoneNumber, string FullName, string Email);
public record LoginRequest(string Username, string Password);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Email, string NewPassword, string Token);
public record RefreshTokenRequest(string AccessToken, string RefreshToken);