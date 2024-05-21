﻿using Microsoft.AspNetCore.Identity;
using Moq;
using Server.Model;

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