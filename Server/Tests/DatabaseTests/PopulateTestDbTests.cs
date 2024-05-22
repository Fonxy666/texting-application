using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Server.Database;
using Server.Model;
using Server.Model.Chat;
using Server.Services.Chat.RoomService;
using Xunit;

namespace Tests.DatabaseTests;

public class PopulateDbAndAddRolesTests
{
    private Mock<IApplicationBuilder> _mockAppBuilder;
    private Mock<IServiceScope> _mockScope;
    private Mock<IServiceScopeFactory> _mockServiceScopeFactory;
    private Mock<IServiceProvider> _mockServiceProvider;
    private Mock<RoleManager<IdentityRole<Guid>>> _mockRoleManager;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager = MockUserManager.Create();
    private Mock<IRoomService> _mockRoomService;
    private Mock<IConfiguration> _mockConfiguration;

    public PopulateDbAndAddRolesTests()
    {
        _mockAppBuilder = new Mock<IApplicationBuilder>();
        _mockScope = new Mock<IServiceScope>();
        _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockRoleManager = new Mock<RoleManager<IdentityRole<Guid>>>(
            Mock.Of<IRoleStore<IdentityRole<Guid>>>(), null, null, null, null);
        _mockRoomService = new Mock<IRoomService>();
        _mockConfiguration = new Mock<IConfiguration>();

        _mockAppBuilder.Setup(x => x.ApplicationServices).Returns(_mockServiceProvider.Object);
        _mockServiceProvider.Setup(x => x.GetService(typeof(IServiceScopeFactory))).Returns(_mockServiceScopeFactory.Object);
        _mockServiceScopeFactory.Setup(x => x.CreateScope()).Returns(_mockScope.Object);
        _mockScope.Setup(x => x.ServiceProvider).Returns(_mockServiceProvider.Object);

        _mockServiceProvider.Setup(x => x.GetService(typeof(RoleManager<IdentityRole<Guid>>))).Returns(_mockRoleManager.Object);
        _mockServiceProvider.Setup(x => x.GetService(typeof(UserManager<ApplicationUser>))).Returns(_mockUserManager.Object);
        _mockServiceProvider.Setup(x => x.GetService(typeof(IRoomService))).Returns(_mockRoomService.Object);
    }

    [Fact]
    public async Task AddRolesAndAdmin_CreatesAdmin_IfNotExist()
    {
        _mockConfiguration.Setup(x => x["AdminEmail"]).Returns("admin@test.com");
        _mockConfiguration.Setup(x => x["AdminUserName"]).Returns("admin");
        _mockConfiguration.Setup(x => x["AdminPassword"]).Returns("adminPassword123");
        _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser)null);
        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);

        await PopulateDbAndAddRoles.AddRolesAndAdmin(_mockAppBuilder.Object, _mockConfiguration.Object);

        _mockUserManager.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Once);
        _mockUserManager.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Admin"), Times.Once);
    }

    [Fact]
    public async Task AddRolesAndAdmin_DoesNotCreateAdmin_IfExist()
    {
        var existingAdmin = new ApplicationUser { Email = "admin@test.com" };
        _mockConfiguration.Setup(x => x["AdminEmail"]).Returns("admin@test.com");
        _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync(existingAdmin);

        await PopulateDbAndAddRoles.AddRolesAndAdmin(_mockAppBuilder.Object, _mockConfiguration.Object);

        _mockUserManager.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
        _mockUserManager.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "Admin"), Times.Never);
    }

    [Fact]
    public async Task CreateTestRoom_DoesNotCreateRoom_IfExist()
    {
        var existingRoom = new Room();
        var roomId = new Guid("901d40c6-c95d-47ed-a21a-88cda341d0a9");
        _mockRoomService.Setup(x => x.GetRoomById(roomId)).ReturnsAsync(existingRoom);

        await PopulateDbAndAddRoles.CreateTestRoom(_mockAppBuilder.Object);

        _mockRoomService.Verify(x => x.RegisterRoomAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task CreateTestUsers_CreatesUsers_IfNotExist()
    {
        _mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((ApplicationUser)null);
        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        
        await PopulateDbAndAddRoles.CreateTestUsers(_mockAppBuilder.Object);

        _mockUserManager.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Exactly(3));
        _mockUserManager.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"), Times.Exactly(3));
    }

    [Fact]
    public async Task CreateTestUsers_DoesNotCreateUsers_IfExist()
    {
        var existingUser = new ApplicationUser { Email = "test1@hotmail.com" };
        _mockUserManager.Setup(x => x.FindByEmailAsync("test1@hotmail.com")).ReturnsAsync(existingUser);
        _mockUserManager.Setup(x => x.FindByEmailAsync("test2@hotmail.com")).ReturnsAsync((ApplicationUser)null);
        _mockUserManager.Setup(x => x.FindByEmailAsync("test3@hotmail.com")).ReturnsAsync((ApplicationUser)null);
        _mockUserManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);

        await PopulateDbAndAddRoles.CreateTestUsers(_mockAppBuilder.Object);

        _mockUserManager.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Exactly(2));
        _mockUserManager.Verify(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"), Times.Exactly(2));
    }
}