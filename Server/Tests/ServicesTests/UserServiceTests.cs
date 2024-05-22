using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using MockQueryable.Moq;
using Moq;
using Server.Model;
using Server.Services.User;
using Xunit;
using Assert = NUnit.Framework.Assert;

namespace Tests.ServicesTests;

public class UserServiceTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly UserServices _userServices;
    private readonly Mock<UserManager<ApplicationUser>> _mockUserManager = MockUserManager.Create();

    public UserServiceTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _userServices = new UserServices(_mockUserManager.Object, _mockConfiguration.Object);
    }

    [Fact]
    public async Task ExistingUser_ShouldReturnTrue_WhenUserExists()
    {
        const string userId = "901d40c6-c95d-47ed-a21a-88cda341d0a9";
        var user = new ApplicationUser
        {
            Id = new Guid(userId)
        };
        var users = new[] { user }.AsQueryable().BuildMockDbSet();
        _mockUserManager.Setup(m => m.Users).Returns(users.Object);

        var result = await _userServices.ExistingUser(userId);

        Assert.True(result);
    }

    [Fact]
    public async Task ExistingUser_ShouldReturnFalse_WhenUserDoesNotExist()
    {
        const string userId = "test-user-id";
        var users = Enumerable.Empty<ApplicationUser>().AsQueryable().BuildMockDbSet();
        _mockUserManager.Setup(m => m.Users).Returns(users.Object);

        var result = await _userServices.ExistingUser(userId);

        Assert.False(result);
    }

    [Fact]
    public void SaveImageLocally_ShouldSaveImage_WhenBase64IsValid()
    {
        const string userNameFileName = "testUser";
        const string base64Image = "iVBORw0KGgoAAAANSUhEUgAAAAUA" +
                                   "AAAFCAYAAACNbyblAAAAHElEQVQI12P4" +
                                   "//8/w38GIAXDIBKE0DHxgljNBAAO" +
                                   "9TXL0Y4OHwAAAABJRU5ErkJggg==";
        
        var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Avatars");
        _mockConfiguration.Setup(c => c["ImageFolderPath"]).Returns(folderPath);

        var imagePath = _userServices.SaveImageLocally(userNameFileName, base64Image);

        Assert.False(string.IsNullOrEmpty(imagePath));
        Assert.True(File.Exists(imagePath));
        File.Delete(imagePath);
    }

    [Fact]
    public void SaveImageLocally_ShouldThrowFormatException_WhenBase64IsInvalid()
    {
        const string userNameFileName = "testUser";
        const string invalidBase64Image = "invalid base64 string";
        var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Avatars");
        _mockConfiguration.Setup(c => c["ImageFolderPath"]).Returns(folderPath);

        Assert.Throws<FormatException>(() => _userServices.SaveImageLocally(userNameFileName, invalidBase64Image));
    }

    [Fact]
    public void GetContentType_ShouldReturnCorrectContentType()
    {
        const string filePath = "test.png";
        
        var contentType = _userServices.GetContentType(filePath);

        Assert.AreEqual("image/png", contentType);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnSuccessResponse_WhenUserIsDeleted()
    {
        var user = new ApplicationUser { UserName = "testUser" };
        _mockUserManager.Setup(m => m.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);

        var response = await _userServices.DeleteAsync(user);

        Assert.True(response.Successful);
        Assert.AreEqual("Delete successful.", response.Message);
        Assert.AreEqual("testUser", response.Username);
    }
}