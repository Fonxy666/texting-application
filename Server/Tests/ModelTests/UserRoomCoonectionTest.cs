using Server.Model;

namespace Tests.ModelTests;

[TestFixture]
public class UserRoomConnectionTests
{
    [Test]
    public void UserRoomConnection_Properties_AreSetCorrectly()
    {
        var user = "John";
        var room = "Lobby";

        var connection = new UserRoomConnection(user, room);
        Assert.Multiple(() =>
        {
            Assert.That(connection.User, Is.EqualTo(user));
            Assert.That(connection.Room, Is.EqualTo(room));
        });
    }

    [Test]
    public void UserRoomConnection_Properties_CanBeNull()
    {
        var connection = new UserRoomConnection("", "");
        Assert.Multiple(() =>
        {
            Assert.That(connection.User, Is.EqualTo(string.Empty));
            Assert.That(connection.Room, Is.EqualTo(string.Empty));
        });
    }
}
