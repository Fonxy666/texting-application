using Server.Model.Chat;

namespace Tests.ModelTests;

[TestFixture]
public class RoomTests
{
    [Test]
    public void Room_Constructor_SetsPropertiesCorrectly()
    {
        const string roomName = "TestRoom";
        const string password = "TestPassword";

        var room = new Room(roomName, password);

        Assert.AreEqual(roomName, room.RoomName);
        Assert.IsNotNull(room.RoomId);
    }

    [Test]
    public void Room_PasswordMatch_ReturnsTrueForCorrectPassword()
    {
        const string roomName = "TestRoom";
        const string password = "TestPassword";
        var room = new Room(roomName, password);

        var passwordMatch = room.PasswordMatch(password);

        Assert.IsTrue(passwordMatch);
    }

    [Test]
    public void Room_PasswordMatch_ReturnsFalseForIncorrectPassword()
    {
        const string roomName = "TestRoom";
        const string password = "TestPassword";
        const string incorrectPassword = "IncorrectPassword";
        var room = new Room(roomName, password);

        var passwordMatch = room.PasswordMatch(incorrectPassword);

        Assert.IsFalse(passwordMatch);
    }
}
