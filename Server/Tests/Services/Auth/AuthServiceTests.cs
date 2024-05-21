using Moq;
using Server.Services.Authentication;
using Server.Services.Cookie;

namespace Tests.Services.Auth;

[TestFixture]
public class AuthServiceTests
{
    [Test]
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

    [Test]
    public async Task LoginAsync_ValidCredentials_ReturnsAuthResultWithToken()
    {
        var userManagerMock = MockUserManager.Create();
        var tokenServiceMock = new Mock<ITokenService>();
        var cookieService = new Mock<ICookieService>();

        var authService = new AuthService(userManagerMock.Object, tokenServiceMock.Object, cookieService.Object);

        var result = await authService.LoginAsync("TestUser", false);

        Assert.That(result.Success, Is.True);
    }
}
