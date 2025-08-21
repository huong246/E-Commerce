using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MockQueryable.Moq;
using Moq;
using SaleManagementRewrite.Data;
using SaleManagementRewrite.Entities;
using SaleManagementRewrite.IServices;
using SaleManagementRewrite.Results;
using SaleManagementRewrite.Schemas;
using SaleManagementRewrite.Services;

namespace SaleManagementRewriteTest;

public class UserServiceTest
{
    [Fact]
    public async Task RegisterUser_WhenValidRequest_ReturnsSuccess()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockConfiguration = new Mock<IConfiguration>();
        var mockMemoryCache = new Mock<IMemoryCache>();
        var request = new RegisterRequest("testUsername1", "testPassword", "123345677", "TheMan", "123455444");
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<User>>();
        var mockSignInManager = new Mock<SignInManager<User>>(
            mockUserManager.Object, 
            mockHttpContextAccessor.Object, 
            claimsFactoryMock.Object, 
            null!, null!, null!, null!);
        var mockEmailService = new Mock<IEmailService>();
        var userService = new UserService(
            mockHttpContextAccessor.Object, 
            mockConfiguration.Object, 
            mockMemoryCache.Object,
            mockUserManager.Object,
            mockSignInManager.Object,
            mockEmailService.Object
        );
        mockUserManager.Setup(x => x.FindByNameAsync(request.Username)).ReturnsAsync((User)null!);
        mockUserManager.Setup(x => x.FindByEmailAsync(request.Email)).ReturnsAsync((User)null!);
        mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), request.Password))
            .ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        var result = await userService.RegisterUser(request);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(request.Username, result.Value.UserName);
        mockUserManager.Verify(x => x.CreateAsync(It.IsAny<User>(), request.Password), Times.Once);
    }
    
    [Fact]
    public async Task RegisterUser_WhenUsernameExists_ReturnsUsernameExists()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var user = new User()
        {
            Id = Guid.NewGuid(),
            Balance = 0,
            FullName = "testUser",
            UserName = "TestUsername",
            PasswordHash = "testPassword",
            PhoneNumber = "12334567711",
            Email = "12345678"
        };
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockConfiguration = new Mock<IConfiguration>();
        var mockMemoryCache = new Mock<IMemoryCache>();
        var request = new RegisterRequest("testUsername", "TestPassword", "123345677", "TheMan", "123455444");
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<User>>();
        var mockSignInManager = new Mock<SignInManager<User>>(
            mockUserManager.Object, 
            mockHttpContextAccessor.Object, 
            claimsFactoryMock.Object, 
            null!, null!, null!, null!);

        var mockEmailService = new Mock<IEmailService>();
        var userService = new UserService(
            mockHttpContextAccessor.Object, 
            mockConfiguration.Object, 
            mockMemoryCache.Object,
            mockUserManager.Object,
            mockSignInManager.Object,
            mockEmailService.Object
        );
        mockUserManager.Setup(x => x.FindByNameAsync(request.Username)).ReturnsAsync(user);
        mockUserManager.Setup(x => x.FindByEmailAsync(request.Email )).ReturnsAsync((User)null!);
        mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), request.Password))
            .ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        var result = await userService.RegisterUser(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task RegisterUser_WhenPasswordLengthNotEnough_ReturnsPasswordLengthNotEnough()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockConfiguration = new Mock<IConfiguration>();
        var mockMemoryCache = new Mock<IMemoryCache>();
        var request = new RegisterRequest("testUsername1", "1", "123345677", "TheMan", "123455444");
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<User>>();
        var mockSignInManager = new Mock<SignInManager<User>>(
            mockUserManager.Object, 
            mockHttpContextAccessor.Object, 
            claimsFactoryMock.Object, 
            null!, null!, null!, null!);

        var mockEmailService = new Mock<IEmailService>();
        var userService = new UserService(
            mockHttpContextAccessor.Object, 
            mockConfiguration.Object, 
            mockMemoryCache.Object,
            mockUserManager.Object,
            mockSignInManager.Object,
            mockEmailService.Object
        );
        mockUserManager.Setup(x => x.FindByNameAsync(request.Username)).ReturnsAsync((User)null!);
        mockUserManager.Setup(x => x.FindByEmailAsync(request.Email )).ReturnsAsync((User)null!);
        mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), request.Password))
            .ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        var result = await userService.RegisterUser(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }
    [Fact]
    public async Task RegisterUser_WhenSaveChangesFails_ReturnsDatabaseError()
    {
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockConfiguration = new Mock<IConfiguration>();
        var mockMemoryCache = new Mock<IMemoryCache>();
        var request = new RegisterRequest("testUsername1", "testPassword", "123345677", "TheMan", "123455444");
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<User>>();
        var mockSignInManager = new Mock<SignInManager<User>>(
            mockUserManager.Object, 
            mockHttpContextAccessor.Object, 
            claimsFactoryMock.Object, 
            null!, null!, null!, null!);

        var mockEmailService = new Mock<IEmailService>();
        var userService = new UserService(
            mockHttpContextAccessor.Object, 
            mockConfiguration.Object, 
            mockMemoryCache.Object,
            mockUserManager.Object,
            mockSignInManager.Object,
            mockEmailService.Object
        );
        mockUserManager.Setup(x => x.FindByNameAsync(request.Username)).ReturnsAsync((User)null!);
        mockUserManager.Setup(x => x.FindByEmailAsync(request.Email )).ReturnsAsync((User)null!);
        mockUserManager.Setup(x => x.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        var dbErrors = new[] { new IdentityError { Code = "DbError", Description = "Database error." } };
        mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), request.Password))
            .ReturnsAsync(IdentityResult.Failed(dbErrors));
        var result = await userService.RegisterUser(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }
}
