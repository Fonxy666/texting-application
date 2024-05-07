using Microsoft.AspNetCore.Identity;
using Moq;
using Server.Model;
using Server.Services.Authentication;
using Server.Services.Cookie;

namespace Tests.Services.Auth
{
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

    internal static class MockUserManager
    {
        public static Mock<UserManager<ApplicationUser>> Create()
        {
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            var userManagerMock = new Mock<UserManager<ApplicationUser>>(userStoreMock.Object, null, null, null, null, null, null, null, null);

            userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                           .ReturnsAsync(IdentityResult.Success);

            userManagerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>()))
                           .ReturnsAsync(new ApplicationUser("example.url")
                           {
                               Id = Guid.NewGuid(),
                               UserName = "TestUser",
                               Email = "test@example.com"
                           });

            userManagerMock.Setup(x => x.CheckPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                           .ReturnsAsync(true);

            userManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>()))
                           .ReturnsAsync(new List<string> { "UserRole" });

            return userManagerMock;
        }
    }
}