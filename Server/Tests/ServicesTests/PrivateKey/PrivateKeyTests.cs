using Microsoft.EntityFrameworkCore;
using Server.Database;
using Server.Model.Requests;
using Server.Services.PrivateKey;

namespace Tests.ServicesTests.PrivateKey;

public class PrivateKeyTests
{
    private DbContextOptions<PrivateKeysDbContext> options;
    
    [SetUp]
    public void Setup()
    {
        options = new DbContextOptionsBuilder<PrivateKeysDbContext>()
            .UseInMemoryDatabase(databaseName: "Test_Database")
            .Options;
    }

    [Test]
    public async Task SendKey_Test()
    {
        await using var context = new PrivateKeysDbContext(options);

        var keyService = new PrivateKeyService(context);
        var requestKey = new Server.Model.PrivateKey(Guid.NewGuid(), "encryptedTestKey", "testIv");
        var result = await keyService.SaveKey(requestKey);
        
        Assert.That(result, Is.True);
    }
    
    [Test]
    public async Task DeleteKey_Test()
    {
        await using var context = new PrivateKeysDbContext(options);

        var keyService = new PrivateKeyService(context);
        var userGuid = Guid.NewGuid();
        var requestKey = new Server.Model.PrivateKey(userGuid, "encryptedTestKey", "testIv");
        var saveResult = await keyService.SaveKey(requestKey);
        
        Assert.That(saveResult, Is.True);

        var deleteResult = await keyService.DeleteKey(userGuid);
        Assert.That(deleteResult, Is.True);
    }
    
    [Test]
    public async Task GetEncryptedKeyByUserIdAsync_Test()
    {
        await using var context = new PrivateKeysDbContext(options);

        var keyService = new PrivateKeyService(context);
        var userGuid = Guid.NewGuid();
        var requestKey = new Server.Model.PrivateKey(userGuid, "encryptedTestKey", "testIv");
        var saveResult = await keyService.SaveKey(requestKey);
        
        Assert.That(saveResult, Is.True);

        var getResult = await keyService.GetEncryptedKeyByUserIdAsync(userGuid);
        Assert.That(getResult, Is.EqualTo(new PrivateKeyResponse("encryptedTestKey", "testIv")));
    }
}