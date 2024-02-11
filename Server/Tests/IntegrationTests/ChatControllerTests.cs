/*using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json;
using Server;
using Server.Model.Chat;
using Server.Requests;
using System.Text;
using Server.Responses;
using Xunit;
using Assert = Xunit.Assert;

public class ChatControllerIntegrationTests
{
    private readonly TestServer _server;
    private readonly HttpClient _client;

    public ChatControllerIntegrationTests()
    {
        // Arrange
        _server = new TestServer(new WebHostBuilder().UseStartup<Startup>());
        _client = _server.CreateClient();
    }

    [Fact]
    public async Task RegisterRoom_ValidRequest_ReturnsOkResult()
    {
        // Act
        var request = new RoomRequest("TestRoom", "TestPassword");
        var jsonRequest = JsonConvert.SerializeObject(request);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/Chat/RegisterRoom", content);

        // Assert
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<RoomResponse>(responseBody);
        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task LoginRoom_ValidRequest_ReturnsOkResult()
    {
        // Act
        var request = new RoomRequest("TestRoom", "TestPassword");
        var jsonRequest = JsonConvert.SerializeObject(request);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/Chat/JoinRoom", content);

        // Assert
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<RoomResponse>(responseBody);
        Assert.NotNull(result);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task GetMessages_ValidRequest_ReturnsOkResult()
    {
        var roomId = "ValidRoomId";

        var response = await _client.GetAsync($"/Chat/GetMessages/{roomId}");

        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<Message[]>(responseBody);
        Assert.NotNull(result);
    }
}*/