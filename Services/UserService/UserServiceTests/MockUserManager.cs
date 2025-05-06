using Microsoft.AspNetCore.Identity;
using Moq;
using UserService.Models;

namespace Tests;

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
                Id = new Guid("901d40c6-c95d-47ed-a21a-88cda341d0a8"),
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