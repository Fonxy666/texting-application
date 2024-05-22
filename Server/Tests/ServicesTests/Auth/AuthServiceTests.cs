using Microsoft.AspNetCore.Identity;
using Moq;
using Server.Model;
using Server.Model.Responses.Auth;
using Server.Services.Authentication;
using Server.Services.Cookie;
using Xunit;
using Xunit.Abstractions;
using Assert = NUnit.Framework.Assert;

namespace Tests.ServicesTests.Auth;

public class AuthServiceTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager = MockUserManager.Create();

    public AuthServiceTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task RegisterAsync_SuccessfulRegistration_ReturnsAuthResultWithToken()
    {
        var userManagerMock = MockUserManager.Create();
        var tokenServiceMock = new Mock<ITokenService>();
        var cookieService = new Mock<ICookieService>();

        var authService = new AuthService(userManagerMock.Object, tokenServiceMock.Object, cookieService.Object);

        var result = await authService.RegisterAsync("test@example.com", "TestUser", "password123", "User", "123456789", "image");

        Assert.That(result.Success, Is.True);
        Assert.That(result.Id, Is.EqualTo(""));
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsAuthResultWithToken()
    {
        var userManagerMock = MockUserManager.Create();
        var tokenServiceMock = new Mock<ITokenService>();
        var cookieService = new Mock<ICookieService>();

        var authService = new AuthService(userManagerMock.Object, tokenServiceMock.Object, cookieService.Object);

        var result = await authService.LoginAsync("TestUser", false);

        Assert.That(result.Success, Is.True);
    }
    
    [Fact]
    public async Task LoginWithExternalAsync_ValidCredentials_ReturnsAuthResultWithToken()
    {
        var tokenServiceMock = new Mock<ITokenService>();
        var cookieServiceMock = new Mock<ICookieService>();

        var authService = new AuthService(_mockUserManager.Object, tokenServiceMock.Object, cookieServiceMock.Object);

        var testEmail = "test@example.com";
        var testUser = new ApplicationUser("testImage")
        {
            UserName = "TestUser",
            Email = testEmail,
            Id = Guid.NewGuid()
        };
        var testRoles = new List<string> { "User" };
        const string testToken = "testToken";

        _mockUserManager.Setup(um => um.FindByEmailAsync(testEmail)).ReturnsAsync(testUser);
        _mockUserManager.Setup(um => um.GetRolesAsync(testUser)).ReturnsAsync(testRoles);
        tokenServiceMock.Setup(ts => ts.CreateJwtToken(testUser, testRoles[0], true)).Returns(testToken);

        var result = await authService.LoginWithExternal(testEmail);

        Assert.True(result.Success);
        Assert.AreEqual(testUser.Id.ToString(), result.Id);

        cookieServiceMock.Verify(cs => cs.SetRefreshToken(testUser), Times.Once);
        cookieServiceMock.Verify(cs => cs.SetRememberMeCookie(true), Times.Once);
        cookieServiceMock.Verify(cs => cs.SetUserId(testUser.Id, true), Times.Once);
        cookieServiceMock.Verify(cs => cs.SetAnimateAndAnonymous(true), Times.Once);
        cookieServiceMock.Verify(cs => cs.SetJwtToken(testToken, true), Times.Once);
        _mockUserManager.Verify(um => um.UpdateAsync(testUser), Times.Once);
    }
    
    [Fact]
    public async Task ExamineLockoutEnabled_AccountLocked_ReturnsInvalidCredentials()
    {
        var user = new ApplicationUser("testImage")
        {
            UserName = "TestUser",
            Email = "test@example.com",
            PasswordHash = "TestPasswordHash"
        };

        var lockoutEndDate = DateTimeOffset.Now.AddMinutes(30);

        _mockUserManager.Setup(um => um.FindByNameAsync(user.UserName)).ReturnsAsync(user);
        _mockUserManager.Setup(um => um.GetLockoutEndDateAsync(user)).ReturnsAsync(lockoutEndDate);

        var authService = CreateAuthService();

        var result = await authService.ExamineLoginCredentials(user.UserName, "TestPassword");
        _testOutputHelper.WriteLine(result.ErrorMessages.Keys.ToString());

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ExamineLockoutEnabled_UserLockout_ReturnsInvalidCredentials()
    {
        var user = new ApplicationUser("testImage")
        {
            UserName = "TestUser",
            Email = "test@example.com",
            PasswordHash = "TestPasswordHash"
        };

        _mockUserManager.Setup(um => um.FindByNameAsync(user.UserName)).ReturnsAsync(user);
        _mockUserManager.Setup(um => um.GetLockoutEndDateAsync(user)).ReturnsAsync((DateTimeOffset?)null);
        _mockUserManager.Setup(um => um.GetAccessFailedCountAsync(user)).ReturnsAsync(4);

        var authService = CreateAuthService();

        var result = await authService.ExamineLoginCredentials(user.UserName, "TestPassword");

        Assert.False(result.Success);

        _mockUserManager.Verify(um => um.SetLockoutEndDateAsync(user, It.IsAny<DateTimeOffset>()), Times.Once);
        _mockUserManager.Verify(um => um.ResetAccessFailedCountAsync(user), Times.Once);
    }
    
    private AuthService CreateAuthService()
    {
        var tokenServiceMock = new Mock<ITokenService>();
        var cookieServiceMock = new Mock<ICookieService>();
        return new AuthService(_mockUserManager.Object, tokenServiceMock.Object, cookieServiceMock.Object);
    }
}
