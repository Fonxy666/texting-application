using Server.Model.Chat;

namespace Tests.ModelTests;

[TestFixture]
public class MessageTests
{
    [Test]
    public void Message_Constructor_SetsPropertiesCorrectly()
    {
        var roomId = "TestRoomId";
        var senderName = "TestSender";
        var text = "TestText";

        var message = new Message(roomId, senderName, text);

        Assert.IsNotNull(message.MessageId);
        Assert.AreEqual(roomId, message.RoomId);
        Assert.AreEqual(senderName, message.SenderName);
        Assert.AreEqual(text, message.Text);
        Assert.IsNotNull(message.SendTime);
    }
}
