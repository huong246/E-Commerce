using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using SaleManagementRewrite.Data;
using SaleManagementRewrite.Entities;
using SaleManagementRewrite.Results;
using SaleManagementRewrite.Schemas;
using SaleManagementRewrite.Services;

namespace SaleManagementRewriteTest;

public class TransactionServiceTest
{
    [Fact]
    public async Task DepositIntoBalance_WhenRequestValid_ReturnsSuccess()
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
            FullName = "John Doe",
            PhoneNumber = "0888888888",
            Balance = 0,
        };
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockConfiguration = new Mock<IConfiguration>();
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);
        var transactionService = new TransactionService(mockHttpContextAccessor.Object, dbContext, mockConfiguration.Object, mockUserManager.Object);
        var request = new DepositIntoBalanceRequest(100000);
        var result = await transactionService.DepositIntoBalanceAsync(request);
        Assert.True(result.IsSuccess);
        Assert.Equal(100000, user.Balance);
    }

    [Fact]
    public async Task DepositIntoBalance_WhenTokenInvalid_ReturnsTokenInvalid()
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
            FullName = "John Doe",
            PhoneNumber = "0888888888",
            Balance = 0,
        };
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockConfiguration = new Mock<IConfiguration>();
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "null") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var transactionService = new TransactionService(mockHttpContextAccessor.Object, dbContext, mockConfiguration.Object, mockUserManager.Object);
        var request = new DepositIntoBalanceRequest(100000);
        var result = await transactionService.DepositIntoBalanceAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Unauthorized, result.ErrorType);
    }

    [Fact]
    public async Task DepositIntoBalance_WhenUserNotFound_ReturnsUserNotFound()
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
            PasswordHash ="123456789", 
            FullName = "John Doe",
            PhoneNumber = "0888888888",
            Balance = 0,
        };
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockConfiguration = new Mock<IConfiguration>();
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier,Guid.NewGuid().ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var transactionService = new TransactionService(mockHttpContextAccessor.Object, dbContext, mockConfiguration.Object, mockUserManager.Object);
        var request = new DepositIntoBalanceRequest(100000);
        var result = await transactionService.DepositIntoBalanceAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task DepositIntoBalance_WhenAmountInvalid_ReturnsAmountInvalid()
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
            FullName = "John Doe",
            PhoneNumber = "0888888888",
            Balance = 0,
        };
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockConfiguration = new Mock<IConfiguration>();
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        var transactionService = new TransactionService(mockHttpContextAccessor.Object, dbContext, mockConfiguration.Object, mockUserManager.Object);
        var request = new DepositIntoBalanceRequest(-100000);
        var result = await transactionService.DepositIntoBalanceAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }
    [Fact]
    public async Task DepositIntoBalance_WhenConcurrencyConflict_ReturnsConcurrencyConflict()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        var dbContext = new Mock<ApiDbContext>(options) { CallBase = true };
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            UserName = "123234567",
            PasswordHash = "123456789", 
            FullName = "John Doe",
            PhoneNumber = "0888888888",
            Balance = 0,
        };
        await dbContext.Object.Database.EnsureCreatedAsync(); 
        await dbContext.Object.Users.AddAsync(user);
        await dbContext.Object.SaveChangesAsync();
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockConfiguration = new Mock<IConfiguration>();
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier,userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        dbContext.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);
        var transactionService = new TransactionService(mockHttpContextAccessor.Object, dbContext.Object, mockConfiguration.Object, mockUserManager.Object);
        var request = new DepositIntoBalanceRequest(100000);
        var result = await transactionService.DepositIntoBalanceAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }

    [Fact]
    public async Task DepositIntoBalance_WhenDatabaseError_ReturnsDatabaseError()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        var dbContext = new Mock<ApiDbContext>(options) { CallBase = true };
        var userId = Guid.NewGuid();
        var user = new User()
        {
            Id = userId,
            UserName = "123234567",
            PasswordHash = "123456789", 
            FullName = "John Doe",
            PhoneNumber = "0888888888",
            Balance = 0,
        };
        await dbContext.Object.Database.EnsureCreatedAsync(); 
        await dbContext.Object.Users.AddAsync(user);
        await dbContext.Object.SaveChangesAsync();
        
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockConfiguration = new Mock<IConfiguration>();
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier,userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext() { User = claimsPrincipal };
        mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        dbContext.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException());
        var userStoreMock = new Mock<IUserStore<User>>();
        var mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        mockUserManager.Setup(x => x.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);
        var transactionService = new TransactionService(mockHttpContextAccessor.Object, dbContext.Object, mockConfiguration.Object, mockUserManager.Object);
        var request = new DepositIntoBalanceRequest(100000);
        var result = await transactionService.DepositIntoBalanceAsync(request);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Conflict, result.ErrorType);
    }
}