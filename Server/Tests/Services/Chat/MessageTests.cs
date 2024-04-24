using Microsoft.EntityFrameworkCore;
using Server.Database;
using Server.Model.Chat;
using Server.Model.Requests.Message;
using Server.Services.Chat.MessageService;

namespace Tests.Services.Chat
{
    [TestFixture]
    public class MessageServiceTests
    {
        private MessagesContext _dbContext;
        private MessageService _messageService;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<MessagesContext>()
                .UseInMemoryDatabase(databaseName: "InMemoryDatabase")
                .Options;

            _dbContext = new MessagesContext(options);

            _messageService = new MessageService(_dbContext);
        }

        [TearDown]
        public void TearDown()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }

        [Test]
        public async Task SendMessage_ValidMessage_ReturnsSuccessResponse()
        {
            var request = new MessageRequest("TestRoom", "TestUser", "Hello, World!", false);

            var response = await _messageService.SendMessage(request);

            Assert.IsTrue(response.Success);
        }

        [Test]
        public async Task GetLast10Messages_ValidRoomId_ReturnsLast10Messages()
        {
            const string roomId = "TestRoom";
            await SeedDatabase(roomId);

            var messages = await _messageService.GetLast10Messages(roomId);

            Assert.IsNotNull(messages);
            Assert.AreEqual(10, messages.Count());
        }

        private async Task SeedDatabase(string roomId)
        {
            for (var i = 1; i <= 15; i++)
            {
                await _dbContext.Messages.AddAsync(new Message(roomId, $"User{i}", $"Message{i}", false));
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}