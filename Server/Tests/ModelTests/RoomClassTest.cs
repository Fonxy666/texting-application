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

        var room = new Room(roomName, password, new Guid("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916"));

        Assert.AreEqual(roomName, room.RoomName);
        Assert.IsNotNull(room.RoomId);
    }

    [Test]
    public void Room_PasswordMatch_ReturnsTrueForCorrectPassword()
    {
        const string roomName = "TestRoom";
        const string password = "TestPassword";
        var room = new Room(roomName, password, new Guid("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916"));

        var passwordMatch = room.PasswordMatch(password);

        Assert.IsTrue(passwordMatch);
    }

    [Test]
    public void Room_PasswordMatch_ReturnsFalseForIncorrectPassword()
    {
        const string roomName = "TestRoom";
        const string password = "TestPassword";
        const string incorrectPassword = "IncorrectPassword";
        var room = new Room(roomName, password, new Guid("38db530c-b6bb-4e8a-9c19-a5cd4d0fa916"));

        var passwordMatch = room.PasswordMatch(incorrectPassword);

        Assert.IsFalse(passwordMatch);
    }
}
