/*
using Server.Model.Chat;

namespace Tests.ModelTests;

[TestFixture]
public class MessageTests
{
    [Test]
    public void Message_Constructor_SetsPropertiesCorrectly()
    {
        var roomId = new Guid("a57f0d67-8670-4789-a580-3b4a3bd3bf9c");
        var senderName = new Guid("a57f0d67-8670-4789-a580-3b4a3bd3bf9c");
        var text = "TestText";

        var message = new Message(roomId, senderName, text, false);

        Assert.Multiple(() =>
        {
            Assert.That(message.Text, Is.EqualTo(text));
        });
        Assert.That(message.SendTime, Is.Not.Null);
    }
}
*/
