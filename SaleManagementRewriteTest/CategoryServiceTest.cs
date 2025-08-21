using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using SaleManagementRewrite.Data;
using SaleManagementRewrite.Entities;
using SaleManagementRewrite.Entities.Enum;
using SaleManagementRewrite.Schemas;
using SaleManagementRewrite.Services;

namespace SaleManagementRewriteTest;

public class CategoryServiceTest
{
    [Fact]
    public void Debug_EFCore_Model_Configuration()
    {
        // 1. Sắp đặt môi trường test y hệt như các test khác
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        using var dbContext = new ApiDbContext(options);

        // 2. Lấy ra "bản thiết kế" chi tiết của model mà EF Core đang sử dụng
        var debugView = dbContext.Model.ToDebugString();

        // 3. In "bản thiết kế" này ra màn hình console của test runner
        Console.WriteLine(debugView);

        // 4. Kiểm tra xem "bản thiết kế" này có thực sự chứa thông tin về "Category" không
        Assert.Contains("Category", debugView);
    }
    [Fact]
    public async Task CreateCategory_WhenRequestValid_ReturnsCategory()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        await using var dbContext = new ApiDbContext(options);
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            UserName = "123234567",
            PasswordHash = "123456789",
            UserRole = UserRoles.Admin,
            FullName = "John Doe",
            PhoneNumber = "0888888888",
        };
        await dbContext.Database.EnsureCreatedAsync();
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.AddToRoleAsync(user, UserRoles.Seller)).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.IsInRoleAsync(user, UserRoles.Admin)).ReturnsAsync(true);
        var categoryService = new CategoryService(dbContext, mockUserManager.Object, mockHttpContextAccessor.Object);
        var request = new CreateCategoryRequest("quan ao");
        var result = await categoryService.CreateCategoryAsync(request);
        Assert.True(result.IsSuccess);
        var category = await dbContext.Categories.FirstOrDefaultAsync(c=>c.Name == "quan ao");
        Assert.NotNull(category);
    }
}