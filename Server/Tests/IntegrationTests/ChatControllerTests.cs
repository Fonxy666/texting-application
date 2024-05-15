﻿using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Server;
using Server.Model.Requests.Auth;
using Server.Model.Requests.Chat;
using Xunit;
using Assert = Xunit.Assert;

namespace Tests.IntegrationTests;

[Collection("Sequential")]
public class ChatControllerTests : IClassFixture<WebApplicationFactory<Startup>>
{
    private readonly AuthRequest _testUser = new ("TestUsername1", "testUserPassword123###");
    private readonly RoomRequest _testRoom = new ("TestRoom1", "TestRoomPassword", "38db530c-b6bb-4e8a-9c19-a5cd4d0fa916");
    private readonly HttpClient _client;
    private readonly TestServer _testServer;

    public ChatControllerTests()
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
        var cookies = TestLogin.Login_With_Test_User(_testUser, _client, "test1@hotmail.com").Result;
        _client.DefaultRequestHeaders.Add("Cookie", cookies);
    }
    
    [Fact]
    public async Task ChatFunctions_ReturnSuccessStatusCode()
    {
        var jsonRequestRegister = JsonConvert.SerializeObject(_testRoom);
        var contentRegister = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var roomRegistrationResponse = await _client.PostAsync("api/v1/Chat/RegisterRoom", contentRegister);
        roomRegistrationResponse.EnsureSuccessStatusCode();

        var jsonRequest = JsonConvert.SerializeObject(new RoomRequest(_testRoom.RoomName, _testRoom.Password, _testRoom.CreatorId));
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        var roomLoginResponse = await _client.PostAsync("api/v1/Chat/JoinRoom", content);
        
        roomLoginResponse.EnsureSuccessStatusCode();
        
        var jsonRequestDelete = JsonConvert.SerializeObject(_testRoom);
        var contentDelete = new StringContent(jsonRequestDelete, Encoding.UTF8, "application/json");

        var deleteRoomResponse = await _client.PostAsync("api/v1/Chat/DeleteRoom", contentDelete);
        deleteRoomResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task CreateRoom_WithTakenRoomName_ReturnBadRequest()
    {
        var jsonRequestRegister = JsonConvert.SerializeObject(new RoomRequest("test", "test", "38db530c-b6bb-4e8a-9c19-a5cd4d0fa916"));
        var contentRegister = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var roomRegistrationResponse = await _client.PostAsync("api/v1/Chat/RegisterRoom", contentRegister);
        Assert.Equal(HttpStatusCode.BadRequest, roomRegistrationResponse.StatusCode);
    }
    
    [Fact]
    public async Task CreateRoom_WithInvalidCredentials_ReturnBadRequest()
    {
        var jsonRequestRegister = JsonConvert.SerializeObject(new RoomRequest("", "", "38db530c-b6bb-4e8a-9c19-a5cd4d0fa916"));
        var contentRegister = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var roomRegistrationResponse = await _client.PostAsync("api/v1/Chat/RegisterRoom", contentRegister);
        Assert.Equal(HttpStatusCode.BadRequest, roomRegistrationResponse.StatusCode);
    }

    [Fact]
    public async Task JoinRoom_WithInvalidCredentials_ReturnBadRequest()
    {
        var jsonRequestRegister = JsonConvert.SerializeObject(new RoomRequest("", "", "38db530c-b6bb-4e8a-9c19-a5cd4d0fa916"));
        var contentRegister = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var roomRegistrationResponse = await _client.PostAsync("api/v1/Chat/JoinRoom", contentRegister);
        Assert.Equal(HttpStatusCode.BadRequest, roomRegistrationResponse.StatusCode);
    }
    
    [Fact]
    public async Task JoinRoom_WithInvalidCredentials_ReturnNotFound()
    {
        var jsonRequestRegister = JsonConvert.SerializeObject(new RoomRequest("wrongRoomName", "asd", "38db530c-b6bb-4e8a-9c19-a5cd4d0fa916"));
        var contentRegister = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var roomRegistrationResponse = await _client.PostAsync("api/v1/Chat/JoinRoom", contentRegister);
        Assert.Equal(HttpStatusCode.NotFound, roomRegistrationResponse.StatusCode);
    }
    
    [Fact]
    public async Task JoinRoom_WithInvalidPassword_ReturnBadRequest()
    {
        var jsonRequestRegister = JsonConvert.SerializeObject(new RoomRequest("test", "asd", "38db530c-b6bb-4e8a-9c19-a5cd4d0fa916"));
        var contentRegister = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var roomRegistrationResponse = await _client.PostAsync("api/v1/Chat/JoinRoom", contentRegister);
        Assert.Equal(HttpStatusCode.BadRequest, roomRegistrationResponse.StatusCode);
    }
    
    [Fact]
    public async Task DeleteRoom_WithInvalidCredentials_ReturnNotFound()
    {
        var jsonRequestRegister = JsonConvert.SerializeObject(new RoomRequest("wrongRoomName", "asd", "38db530c-b6bb-4e8a-9c19-a5cd4d0fa916"));
        var contentRegister = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var roomRegistrationResponse = await _client.PostAsync("api/v1/Chat/DeleteRoom", contentRegister);
        Assert.Equal(HttpStatusCode.NotFound, roomRegistrationResponse.StatusCode);
    }
    
    [Fact]
    public async Task DeleteRoom_WithInvalidCredentials_ReturnBadRequest()
    {
        var jsonRequestRegister = JsonConvert.SerializeObject(new RoomRequest("", "", ""));
        var contentRegister = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var roomRegistrationResponse = await _client.PostAsync("api/v1/Chat/DeleteRoom", contentRegister);
        Assert.Equal(HttpStatusCode.BadRequest, roomRegistrationResponse.StatusCode);
    }
    
    [Fact]
    public async Task DeleteRoom_WithInvalidPassword_ReturnBadRequest()
    {
        var jsonRequestRegister = JsonConvert.SerializeObject(new RoomRequest("test", "wrongPassword", "38db530c-b6bb-4e8a-9c19-a5cd4d0fa916"));
        var contentRegister = new StringContent(jsonRequestRegister, Encoding.UTF8, "application/json");

        var roomRegistrationResponse = await _client.PostAsync("api/v1/Chat/DeleteRoom", contentRegister);
        Assert.Equal(HttpStatusCode.BadRequest, roomRegistrationResponse.StatusCode);
    }
}