using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using MockQueryable.Moq;
using Moq;
using SaleManagementRewrite.Data;
using SaleManagementRewrite.Entities;
using SaleManagementRewrite.IServices;
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
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        await using var dbContext = new ApiDbContext(options);
        await dbContext.Database.EnsureCreatedAsync(); 
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockConfiguration = new Mock<IConfiguration>();
        var mockMemoryCache = new Mock<IMemoryCache>();
        var request = new RegisterRequest("testUsername", "testPassword", "123345677", "TheMan");
        var userService = new UserService(
            dbContext, 
            mockHttpContextAccessor.Object, 
            mockConfiguration.Object, 
            mockMemoryCache.Object
        );
        var result = await userService.RegisterUser(request);
        Assert.Equal(RegisterRequestResult.Success, result);
        var user = await dbContext.Users.FirstOrDefaultAsync(u=>u.Username == request.Username);
        Assert.NotNull(user);
        Assert.Equal("TheMan", user.FullName);
    }
    
    [Fact]
    public async Task RegisterUser_WhenUsernameExists_ReturnsUsernameExists()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        await using var dbContext = new ApiDbContext(options);
        var user = new User()
        {
            Id = Guid.NewGuid(),
            Balance = 0,
            FullName = "testUser",
            Username = "TestUsername",
            Password = "TestPassword",
            PhoneNumber = "12334567711",
        };
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.Users.AddAsync(user);
        await dbContext.SaveChangesAsync();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockConfiguration = new Mock<IConfiguration>();
        var mockMemoryCache = new Mock<IMemoryCache>();
        var userService = new UserService(
            dbContext, 
            mockHttpContextAccessor.Object, 
            mockConfiguration.Object, 
            mockMemoryCache.Object
        );
        var request = new RegisterRequest("TestUsername", "testPassword", "123345677", "TheMan");
        var result = await userService.RegisterUser(request);
        Assert.Equal(RegisterRequestResult.UsernameExists, result);
        Assert.Equal(1, dbContext.Users.Count());
    }

    [Fact]
    public async Task RegisterUser_WhenPasswordLengthNotEnough_ReturnsPasswordLengthNotEnough()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApiDbContext>().UseSqlite(connection).Options;
        await using var dbContext = new ApiDbContext(options);
        await dbContext.Database.EnsureCreatedAsync(); 
        await dbContext.SaveChangesAsync();
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockConfiguration = new Mock<IConfiguration>();
        var mockMemoryCache = new Mock<IMemoryCache>();
        var userService = new UserService(
            dbContext, 
            mockHttpContextAccessor.Object, 
            mockConfiguration.Object, 
            mockMemoryCache.Object
        );
        var request = new RegisterRequest("TestUsername", "Pass", "123345677", "TheMan");
        var result = await userService.RegisterUser(request);
        Assert.Equal(RegisterRequestResult.PasswordLengthNotEnough, result);
        Assert.Equal(0, dbContext.Users.Count());
    }
    [Fact]
    public async Task RegisterUser_WhenSaveChangesFails_ReturnsDatabaseError()
    {

        var users = new List<User>().AsQueryable();
        var mockDbSet = users.BuildMockDbSet(); 

        var mockDbContext = new Mock<ApiDbContext>(new DbContextOptions<ApiDbContext>());
        mockDbContext.Setup(db => db.Users).Returns(mockDbSet.Object);
        mockDbContext.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("Simulated database error"));

        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockConfiguration = new Mock<IConfiguration>();
        var mockMemoryCache = new Mock<IMemoryCache>();

        var userService = new UserService(
            mockDbContext.Object, 
            mockHttpContextAccessor.Object, 
            mockConfiguration.Object, 
            mockMemoryCache.Object
        );
        var request = new RegisterRequest("testUsername", "testPassword", "123345677", "TheMan");
        
        var result = await userService.RegisterUser(request);
        Assert.Equal(RegisterRequestResult.DatabaseError, result);
        mockDbSet.Verify(m => m.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once());
        mockDbContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }
}