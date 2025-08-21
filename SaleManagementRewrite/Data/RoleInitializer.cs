using Microsoft.AspNetCore.Identity;
using SaleManagementRewrite.Entities;
using SaleManagementRewrite.Entities.Enum;

namespace SaleManagementRewrite.Data;
public class AdminUserInfo 
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } =  string.Empty;
}
public static class RoleInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        
        string[] roleNames = { UserRoles.Admin, UserRoles.Seller, UserRoles.Customer };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            }
        }
        var adminUsers = configuration.GetSection("InitialAdminUsers").Get<List<AdminUserInfo>>();

        if (adminUsers != null)
        {
            foreach (var adminInfo in adminUsers)
            {
                if (await userManager.FindByEmailAsync(adminInfo.Email) != null) continue;
                var adminUser = new User
                {
                    UserName = adminInfo.UserName,
                    Email = adminInfo.Email,
                    EmailConfirmed = true,
                    FullName = adminInfo.FullName
                };
                var result = await userManager.CreateAsync(adminUser, adminInfo.Password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, UserRoles.Admin);
                }
            }
        }
    }
}