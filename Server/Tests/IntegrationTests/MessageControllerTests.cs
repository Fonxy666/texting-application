﻿using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using Server;
using Server.Model.Requests.Auth;
using Server.Model.Requests.Message;
using Xunit;
using Assert = Xunit.Assert;

namespace Tests.IntegrationTests;

[Collection("Sequential")]
public class MessageControllerTests : IClassFixture<WebApplicationFactory<Startup>>
{
    private readonly WebApplicationFactory<Startup> _factory;
    private readonly HttpClient _client;
    private readonly AuthRequest _testUser = new ("TestUsername1", "testUserPassword123###");

    public MessageControllerTests(WebApplicationFactory<Startup> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        var cookies = TestLogin.Login_With_Test_User(_testUser, _client, "test1@hotmail.com").Result;
        _client.DefaultRequestHeaders.Add("Cookie", cookies);
    }
    
    [Fact]
    public async Task GetMessage_WithInvalidRoomId_ReturnsBadRequest()
    {
        const string roomId = "123";

        var getUserResponse = await _client.GetAsync($"/Message/getMessages/{roomId}");
        
        var responseContent = await getUserResponse.Content.ReadAsStringAsync();
        
        Assert.Contains($"There is no room with this id: {roomId}", responseContent);
    }
    
    [Fact]
    public async Task GetMessage_WithValidId_ReturnSuccessStatusCode()
    {
        const string roomId = "858f76ec-9dec-438a-9e63-72287a69f4d2";

        var getUserResponse = await _client.GetAsync($"/Message/getMessages/{roomId}");
        getUserResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task SendMessage_WithValidRequest_ReturnSuccessStatusCode()
    {
        var messageRequest = new MessageRequest("5f843042-f674-4539-ae39-28d722c6c959", "38db530c-b6bb-4e8a-9c19-a5cd4d0fa916", "test", false, "a57f0d67-8670-4789-a580-3b4a3bd3bf9c");
        var jsonRequestMessageSend = JsonConvert.SerializeObject(messageRequest);
        var contentSend = new StringContent(jsonRequestMessageSend, Encoding.UTF8, "application/json");

        var roomRegistrationResponse = await _client.PostAsync("/Message/SendMessage", contentSend);
        roomRegistrationResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task SendMessage_WithInvalidModelState_ReturnBadRequest()
    {
        var messageRequest = "";
        var jsonRequestMessageSend = JsonConvert.SerializeObject(messageRequest);
        var contentSend = new StringContent(jsonRequestMessageSend, Encoding.UTF8, "application/json");

        var sendMessageResponse = await _client.PostAsync("/Message/SendMessage", contentSend);
        
        Assert.Equal(HttpStatusCode.BadRequest, sendMessageResponse.StatusCode);
    }
    
    [Fact]
    public async Task SendMessage_WithNotValidRoomId_ReturnBadRequest()
    {
        var messageRequest = new MessageRequest("123", _testUser.UserName, "test", false, "a57f0d67-8670-4789-a580-3b4a3bd3bf9c");
        var jsonRequestMessageSend = JsonConvert.SerializeObject(messageRequest);
        var contentSend = new StringContent(jsonRequestMessageSend, Encoding.UTF8, "application/json");

        var sendMessageResponse = await _client.PostAsync("/Message/SendMessage", contentSend);
        
        Assert.Equal(HttpStatusCode.NotFound, sendMessageResponse.StatusCode);
    }
    
    [Fact]
    public async Task SendMessage_WithNotValidUserId_ReturnBadRequest()
    {
        var messageRequest = new MessageRequest("5f843042-f674-4539-ae39-28d722c6c959", _testUser.UserName, "test", false, "123");
        var jsonRequestMessageSend = JsonConvert.SerializeObject(messageRequest);
        var contentSend = new StringContent(jsonRequestMessageSend, Encoding.UTF8, "application/json");

        var sendMessageResponse = await _client.PostAsync("/Message/SendMessage", contentSend);
        
        Assert.Equal(HttpStatusCode.NotFound, sendMessageResponse.StatusCode);
    }
    
    [Fact]
    public async Task EditMessage_WithValidCredentials_ReturnSuccessStatusCode()
    {
        var messageChangeRequest = new EditMessageRequest("a57f0d67-8670-4789-a580-3b4a3bd3bf9c", "TestChange");
        var jsonMessageChangeRequest = JsonConvert.SerializeObject(messageChangeRequest);
        var messageChange = new StringContent(jsonMessageChangeRequest, Encoding.UTF8, "application/json");

        var getUserResponse = await _client.PatchAsync("/Message/EditMessage", messageChange);
        getUserResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task EditMessage_WithNotExistingId_ReturnNotFound()
    {
        var messageChangeRequest = new EditMessageRequest("1", "TestChange");
        var jsonMessageChangeRequest = JsonConvert.SerializeObject(messageChangeRequest);
        var messageChange = new StringContent(jsonMessageChangeRequest, Encoding.UTF8, "application/json");

        var getUserResponse = await _client.PatchAsync("/Message/EditMessage", messageChange);
        Assert.Equal(HttpStatusCode.NotFound, getUserResponse.StatusCode);
    }
    
    [Fact]
    public async Task EditMessage_WithInvalidModelState_ReturnBadRequest()
    {
        const string messageChangeRequest = "";
        var jsonMessageChangeRequest = JsonConvert.SerializeObject(messageChangeRequest);
        var messageChange = new StringContent(jsonMessageChangeRequest, Encoding.UTF8, "application/json");

        var editMessageRequest = await _client.PatchAsync("/Message/EditMessage", messageChange);
        
        Assert.Equal(HttpStatusCode.BadRequest, editMessageRequest.StatusCode);
    }
    
    [Fact]
    public async Task EditMessageSeen_WithValidRequest_ReturnSuccessStatusCode()
    {
        var messageChangeRequest = new EditMessageSeenRequest("a57f0d67-8670-4789-a580-3b4a3bd3bf9c", "38db530c-b6bb-4e8a-9c19-a5cd4d0fa916");
        var jsonMessageChangeRequest = JsonConvert.SerializeObject(messageChangeRequest);
        var messageChange = new StringContent(jsonMessageChangeRequest, Encoding.UTF8, "application/json");

        var editMessageRequest = await _client.PatchAsync("/Message/EditMessageSeen", messageChange);
        editMessageRequest.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task EditMessageSeen_WithInvalidMessageId_ReturnNotFound()
    {
        var messageChangeRequest = new EditMessageSeenRequest("1", "38db530c-b6bb-4e8a-9c19-a5cd4d0fa916");
        var jsonMessageChangeRequest = JsonConvert.SerializeObject(messageChangeRequest);
        var messageChange = new StringContent(jsonMessageChangeRequest, Encoding.UTF8, "application/json");

        var editMessageRequest = await _client.PatchAsync("/Message/EditMessageSeen", messageChange);
        Assert.Equal(HttpStatusCode.NotFound, editMessageRequest.StatusCode);
    }
    
    [Fact]
    public async Task EditMessageSeen_WithInvalidModel_ReturnBadRequest()
    {
        const string messageChangeRequest = "";
        var jsonMessageChangeRequest = JsonConvert.SerializeObject(messageChangeRequest);
        var messageChange = new StringContent(jsonMessageChangeRequest, Encoding.UTF8, "application/json");

        var editMessageRequest = await _client.PatchAsync("/Message/EditMessageSeen", messageChange);
        Assert.Equal(HttpStatusCode.BadRequest, editMessageRequest.StatusCode);
    }
    
    [Fact]
    public async Task DeleteMessage_WithValidMessageId_ReturnSuccessStatusCode()
    {
        const string messageDeleteRequestId = "a57f0d67-8670-4789-a580-3b4a3bd3bf9c";

        var getUserResponse = await _client.DeleteAsync($"/Message/DeleteMessage?id={messageDeleteRequestId}");
        getUserResponse.EnsureSuccessStatusCode();
    }
    
    [Fact]
    public async Task DeleteMessage_WithInvalidModel_ReturnBadRequest()
    {
        var messageDeleteRequestId = new EditMessageSeenRequest("", "");

        var getUserResponse = await _client.DeleteAsync($"/Message/DeleteMessage?id={messageDeleteRequestId}");
        Assert.Equal(HttpStatusCode.NotFound, getUserResponse.StatusCode);
    }
}