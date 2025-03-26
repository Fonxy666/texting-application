using AuthenticationService.Model;
using AuthenticationService.Model.Requests.Auth;
using AuthenticationService.Services.Authentication;
using AuthenticationService.Services.Cookie;
using AuthenticationService.Services.PrivateKeyService;
using Microsoft.AspNetCore.Identity;
using MockQueryable.Moq;
using Moq;
using Server.Model;
using Server.Model.Requests.Auth;
using Server.Services.Authentication;
using Server.Services.Cookie;
using Server.Services.PrivateKey;
using Xunit;
using Xunit.Abstractions;
using Assert = NUnit.Framework.Assert;

namespace Tests.ServicesTests.Auth;

public class AuthServiceTests(ITestOutputHelper testOutputHelper)
{
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager = MockUserManager.Create();

    [Fact]
    public async Task RegisterAsync_SuccessfulRegistration_ReturnsAuthResultWithToken()
    {
        var userManagerMock = MockUserManager.Create();
        var tokenServiceMock = new Mock<ITokenService>();
        var cookieService = new Mock<ICookieService>();
        var keyService = new Mock<IPrivateKeyService>();

        const string senderName = "TestUser";
        var senderId = Guid.NewGuid().ToString();

        var applicationUser1 = new ApplicationUser { UserName = senderName, Id = Guid.Parse(senderId), PublicKey = "publidsadascKey" };
        var users = new List<ApplicationUser> { applicationUser1 }.AsQueryable().BuildMock();

        userManagerMock.Setup(um => um.Users).Returns(users);
        userManagerMock.Setup(um => um.FindByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((string _) => users.FirstOrDefault(u => u.UserName == "TestUser"));

        userManagerMock.Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        userManagerMock.Setup(um => um.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        keyService.Setup(ks => ks.SaveKeyAsync(It.IsAny<PrivateKey>()))
            .ReturnsAsync(true);

        var authService = new AuthService(userManagerMock.Object, tokenServiceMock.Object, cookieService.Object, keyService.Object);
        var regRequest = new RegistrationRequest("test@example.com", "TestUser", "passwordD123!!!", "image", "123456789",
            "publidsadascKey", "privadsadsateKey", "ivdsadas");

        var result = await authService.RegisterAsync(regRequest, "User", regRequest.Image);

        Assert.That(result.Success, Is.True);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsAuthResultWithToken()
    {
        var userManagerMock = MockUserManager.Create();
        var tokenServiceMock = new Mock<ITokenService>();
        var cookieService = new Mock<ICookieService>();
        var keyService = new Mock<IPrivateKeyService>();

        var authService = new AuthService(userManagerMock.Object, tokenServiceMock.Object, cookieService.Object, keyService.Object);

        var result = await authService.LoginAsync("TestUser", false);

        Assert.That(result.Success, Is.True);
    }
    
    [Fact]
    public async Task LoginWithExternalAsync_ValidCredentials_ReturnsAuthResultWithToken()
    {
        var tokenServiceMock = new Mock<ITokenService>();
        var cookieServiceMock = new Mock<ICookieService>();
        var keyService = new Mock<IPrivateKeyService>();

        var authService = new AuthService(_mockUserManager.Object, tokenServiceMock.Object, cookieServiceMock.Object, keyService.Object);

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
        testOutputHelper.WriteLine(result.ErrorMessages.Keys.ToString());

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
        var keyService = new Mock<IPrivateKeyService>();
        return new AuthService(_mockUserManager.Object, tokenServiceMock.Object, cookieServiceMock.Object, keyService.Object);
    }
}
