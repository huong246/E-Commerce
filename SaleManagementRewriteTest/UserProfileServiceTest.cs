using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.EntityFrameworkCore;
using SaleManagementRewrite.Data;
using SaleManagementRewrite.Entities;
using SaleManagementRewrite.IServices;
using SaleManagementRewrite.Results;
using SaleManagementRewrite.Schemas;
using SaleManagementRewrite.Services;

namespace SaleManagementRewriteTest;

public class UserProfileServiceTest
{
    [Fact]
    public async Task GetUserProfileAsync_WhenRequestValid_ReturnsUserProfile()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            UserName = "123234567",
            PasswordHash = "11223456767",
            FullName = "John Doe",
            PhoneNumber = "0888888888",
        };

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);

        var userProfileService = new UserProfileService(
            mockHttpContextAccessor.Object, mockUserManager.Object);
        var result = await userProfileService.GetUserProfileAsync();
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(user.UserName, result.Value.UserName);
        Assert.Equal(user.Id, result.Value.Id);
    }

    [Fact]
    public async Task GetUserProfileAsync_WhenTokenInvalid_ReturnsNull()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            UserName = "123234567",
            PasswordHash = "124356788",
            FullName = "John Doe",
            PhoneNumber = "0888888888",
        };

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "null") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);

        var userProfileService = new UserProfileService(
            mockHttpContextAccessor.Object, mockUserManager.Object);
        var result = await userProfileService.GetUserProfileAsync();
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task UpdateUserProfileAsync_WhenRequestValid_ReturnsUserProfile()
    {
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            UserName = "123234567",
            PasswordHash = "124565767",
            FullName = "John Doe",
            PhoneNumber = "0888888888",
            Email = "12345678",
        };
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
        mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((User)null!);
        mockUserManager.Setup(x => x.SetEmailAsync(It.IsAny<User>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.SetPhoneNumberAsync(user, It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        var userProfileService = new UserProfileService(
            mockHttpContextAccessor.Object, mockUserManager.Object);
        var request = new UpdateUserProfileRequest("TestFullName", "TestEmail...", "123456788", new DateTime (1999, 1, 1), "male");
        var result = await userProfileService.UpdateUserProfileAsync(request);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task UpdateUserProfileAsync_WhenTokenInvalid_ReturnsNull()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            UserName = "123234567",
            PasswordHash = "123456789",
            FullName = "John Doe",
            PhoneNumber = "0888888888",
        };

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier,"null")};
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        var userProfileService = new UserProfileService(
            mockHttpContextAccessor.Object, mockUserManager.Object);
        var request = new UpdateUserProfileRequest("TestFullName", "TestEmail...", "123456788", new DateTime (1999, 1, 1), "male");
        var result = await userProfileService.UpdateUserProfileAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task UpdateUserProfileAsync_WhenUserNotFound_ReturnsUserNotFound()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier,  Guid.NewGuid().ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var userProfileService = new UserProfileService(
            mockHttpContextAccessor.Object, mockUserManager.Object);
        var request = new UpdateUserProfileRequest("TestFullName", "TestEmail...", "123456788", new DateTime (1999, 1, 1), "male");
        var result = await userProfileService.UpdateUserProfileAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task UpdateUserProfileAsync_WhenDuplicateValue_ReturnsDuplicateValue()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            UserName = "123234567",
            PasswordHash = "12345678",
            FullName = "John Doe",
            PhoneNumber = "0888888888",
            Email = "12345678",
            Birthday = new DateTime(1999, 1, 1),
            Gender = "male",
        };

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
        var userProfileService = new UserProfileService(
            mockHttpContextAccessor.Object, mockUserManager.Object);
        var request = new UpdateUserProfileRequest("John Doe", "12345678", "0888888888", new DateTime (1999, 1, 1), "male");
        var result = await userProfileService.UpdateUserProfileAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task UpdateUserProfileAsync_WhenDatabaseError_ReturnsDatabaseError()
    {
        var userId = Guid.NewGuid();
        var user = new User()
         {
             Id = userId,
             UserName = "123234567",
             PasswordHash = "123456789",
             FullName = "John Doe",
             PhoneNumber = "0888888888",
             Email = "12345678",
             Birthday = new DateTime(1999, 1, 1),
             Gender = "male",
         };
        var request = new UpdateUserProfileRequest("John Doe", "12345678000", "0888888888", new DateTime (1999, 1, 1), "male");
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((User)null!);
        mockUserManager.Setup(x => x.SetEmailAsync(It.IsAny<User>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        mockUserManager.Setup(x => x.SetPhoneNumberAsync(user, It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        var dbErrors = new[] 
        { 
            new IdentityError { Code = "DbError", Description = "Database error." } 
        };
        mockUserManager.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Failed(dbErrors));
        var userProfileService = new UserProfileService(
            mockHttpContextAccessor.Object, mockUserManager.Object);
        var result = await userProfileService.UpdateUserProfileAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);

    }
    
    [Fact]
    public async Task UpdatePasswordAsync_WhenRequestValid_ReturnsSuccess()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            UserName = "123234567",
            PasswordHash = "123456789",
            FullName = "John Doe",
            PhoneNumber = "0888888888",
            Email = "12345678",
            Birthday = new DateTime(1999, 1, 1),
            Gender = "male",
        };

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
        mockUserManager.Setup(x => x.CheckPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(true);
        mockUserManager.Setup(x => x.ChangePasswordAsync(user, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        var userProfileService = new UserProfileService(
            mockHttpContextAccessor.Object, mockUserManager.Object);
        var request = new UpdatePasswordRequest("123456789", "0909876462");
        var result = await userProfileService.UpdatePasswordAsync(request);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task UpdatePasswordAsync_WhenTokenInvalid_ReturnsTokenInvalid()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            UserName = "123234567",
            PasswordHash = "123456789",
            FullName = "John Doe",
            PhoneNumber = "0888888888",
            Email = "12345678",
            Birthday = new DateTime(1999, 1, 1),
            Gender = "male",
        };

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "null") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.CheckPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(true);
        mockUserManager.Setup(x => x.ChangePasswordAsync(user, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        var userProfileService = new UserProfileService(
            mockHttpContextAccessor.Object, mockUserManager.Object);
        var request = new UpdatePasswordRequest("123456789", "0909876462");
        var result = await userProfileService.UpdatePasswordAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task UpdatePasswordAsync_WhenUserNotFound_ReturnsNotFound()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var userId = Guid.NewGuid();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync((User)null!);
        var userProfileService = new UserProfileService(
            mockHttpContextAccessor.Object, mockUserManager.Object);
        var request = new UpdatePasswordRequest("123456789", "0909876462");
        var result = await userProfileService.UpdatePasswordAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task UpdatePasswordAsync_WhenOldPasswordWrong_returnsOldPasswordWrong()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            UserName = "123234567",
            PasswordHash = "123456789",
            FullName = "John Doe",
            PhoneNumber = "0888888888",
            Email = "12345678",
            Birthday = new DateTime(1999, 1, 1),
            Gender = "male",
        };
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
        mockUserManager.Setup(x => x.CheckPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(false);
        mockUserManager.Setup(x => x.ChangePasswordAsync(user, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        var userProfileService = new UserProfileService(
            mockHttpContextAccessor.Object, mockUserManager.Object);
        var request = new UpdatePasswordRequest("123456789101", "0909876462");
        var result = await userProfileService.UpdatePasswordAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task UpdatePasswordAsync_WhenDuplicateValue_returnsDuplicateValue()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            UserName = "123234567",
            PasswordHash = "123456789",
            FullName = "John Doe",
            PhoneNumber = "0888888888",
            Email = "12345678",
            Birthday = new DateTime(1999, 1, 1),
            Gender = "male",
        };

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
        mockUserManager.Setup(x => x.CheckPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(true);
        mockUserManager.Setup(x => x.ChangePasswordAsync(user, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        var userProfileService = new UserProfileService(
            mockHttpContextAccessor.Object, mockUserManager.Object);
        var request = new UpdatePasswordRequest("123456789", "123456789");
        var result = await userProfileService.UpdatePasswordAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task UpdatePasswordAsync_WhenDatabaseError_ReturnsDatabaseError()
    {
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            UserName = "123234567",
            PasswordHash = "123456789",
            FullName = "John Doe",
            PhoneNumber = "0888888888",
            Email = "12345678",
            Birthday = new DateTime(1999, 1, 1),
            Gender = "male",
        };

        var request = new UpdatePasswordRequest("123456789", "123456455789");
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.CheckPasswordAsync(user, It.IsAny<string>())).ReturnsAsync(true);
        var dbErrors = new[] { new IdentityError { Code = "DbError", Description = "Database error." } };
        mockUserManager.Setup(x => x.ChangePasswordAsync(user, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(dbErrors));
        var userProfileService = new UserProfileService(
            mockHttpContextAccessor.Object, mockUserManager.Object);
        var result = await userProfileService.UpdatePasswordAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }
}
