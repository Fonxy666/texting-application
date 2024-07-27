using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Server;
using Server.Model.Requests.Auth;
using Server.Model.Requests.EncryptKey;
using Xunit;
using Assert = Xunit.Assert;

namespace Tests.IntegrationTests;

public class PrivateKeyTests
{
    private readonly AuthRequest _testUser1 = new ("TestUsername1", "testUserPassword123###");
    private readonly AuthRequest _testUser3 = new ("TestUsername3", "testUserPassword123###");
    private readonly HttpClient _client;
    private readonly TestServer _testServer;

    public PrivateKeyTests()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("testConfiguration.json")
            .Build();
        
        var builder = new WebHostBuilder()
            .UseEnvironment("Test")
            .UseStartup<Startup>()
            .ConfigureAppConfiguration(config =>
            {
                config.AddConfiguration(configuration);
            });

        _testServer = new TestServer(builder);
        _client = _testServer.CreateClient();
    }
    
    [Fact]
    public async Task GetPrivateKeyAndIv_WithValidUser_ReturnsSuccess()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");
        _client.DefaultRequestHeaders.Add("Cookie", cookies);

        var getPrivateKeyResponse = await _client.GetAsync("api/v1/CryptoKey/GetPrivateKeyAndIv");
        getPrivateKeyResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task GetPrivateUserKey_WithValidRoomId_ReturnsSuccess()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");
        _client.DefaultRequestHeaders.Add("Cookie", cookies);
        const string roomId = "901d40c6-c95d-47ed-a21a-88cda341d0a9";

        var getPrivateKeyResponse = await _client.GetAsync($"api/v1/CryptoKey/GetPrivateUserKey?roomId={roomId}");
        getPrivateKeyResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task GetPrivateUserKey_WithValidRoomId_WithOutKey_ReturnsNotFound()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");
        _client.DefaultRequestHeaders.Add("Cookie", cookies);
        const string roomId = "901d40c6-c95d-47ed-a21a-88cda311d0a9";

        var getPrivateKeyResponse = await _client.GetAsync($"api/v1/CryptoKey/GetPrivateUserKey?roomId={roomId}");
        Assert.Equal(HttpStatusCode.NotFound, getPrivateKeyResponse.StatusCode);
    }
    
    [Fact]
    public async Task SaveEncryptedRoomKey_WithValidCredentials_ReturnsSuccess()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");
        _client.DefaultRequestHeaders.Add("Cookie", cookies);
        
        var data = new StoreRoomKeyRequest("testEncryptedKey", "901d40c6-c95d-47ed-a21a-88cda341d0a9");
        var jsonRequestMessageSend = JsonConvert.SerializeObject(data);
        var contentSend = new StringContent(jsonRequestMessageSend, Encoding.UTF8, "application/json");

        var savePrivateKeyResponse = await _client.PostAsync("api/v1/CryptoKey/SaveEncryptedRoomKey", contentSend);
        savePrivateKeyResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task SaveEncryptedRoomKey_WithInvalidCredentials_ReturnsBadRequest()
    {
        var cookies = await TestLogin.Login_With_Test_User(_testUser1, _client, "test1@hotmail.com");
        _client.DefaultRequestHeaders.Add("Cookie", cookies);
        
        var data = new StoreRoomKeyRequest("testEncryptedKey", "901d40c6-c95d-47ed-a21a-88cda141d0a9");
        var jsonRequestMessageSend = JsonConvert.SerializeObject(data);
        var contentSend = new StringContent(jsonRequestMessageSend, Encoding.UTF8, "application/json");

        var savePrivateKeyResponse = await _client.PostAsync("api/v1/CryptoKey/SaveEncryptedRoomKey", contentSend);
        Assert.Equal(HttpStatusCode.BadRequest, savePrivateKeyResponse.StatusCode);
    }
}