using Microsoft.EntityFrameworkCore;
using Server.Database;
using Server.Model.Chat;
using Server.Model.Requests.Message;
using Server.Services.Chat.MessageService;

namespace Tests.ServicesTests.Chat
{
    [TestFixture]
    public class MessageServiceTests
    {
        private DatabaseContext _dbContext;
        private MessageService _messageService;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<DatabaseContext>()
                .UseInMemoryDatabase(databaseName: "InMemoryDatabase")
                .Options;

            _dbContext = new DatabaseContext(options);

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
            var request = new MessageRequest("a57f0d67-8670-4789-a580-3b4a3bd3bf9c", "Hello, World!", false, null);

            var response = await _messageService.SendMessage(request, "a57f0d67-8670-4789-a580-3b4a3bd3bf9c");

            Assert.IsTrue(response.Success);
        }
        
        [Test]
        public async Task EditMessage_WithNotTheCreator_ReturnFalse()
        {
            var request = new MessageRequest("a57f0d67-8670-4789-a580-3b4a3bd3bf9c", "Hello, World!", false, null);

            await _messageService.SendMessage(request, "a57f0d67-8670-4789-a580-3b4a3bd3bf9c");

            var message = await _messageService.GetLast10Messages(new Guid("a57f0d67-8670-4789-a580-3b4a3bd3bf9c"));

            var messageId = message.ToList()[0].MessageId;
            var result = await _messageService.UserIsTheSender(new Guid("a57f0d67-8670-4789-a580-3b4a3bd3bf9d"), messageId);

            Assert.IsFalse(result);
        }

        [Test]
        public async Task GetLast10Messages_ValidRoomId_ReturnsLast10Messages()
        {
            var roomId = new Guid("a57f0d67-8670-4789-a580-3b4a3bd3bf9c");
            await SeedDatabase(roomId);

            var messages = await _messageService.GetLast10Messages(roomId);

            Assert.IsNotNull(messages);
            Assert.AreEqual(10, messages.Count());
        }

        private async Task SeedDatabase(Guid roomId)
        {
            for (var i = 1; i <= 15; i++)
            {
                await _dbContext.Messages.AddAsync(new Message(roomId, new Guid("a57f0d67-8670-4789-a580-3b4a3bd3bf9c"), $"Message{i}", false));
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}