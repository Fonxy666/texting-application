using Server.Model;

namespace Tests.ModelTests
{
    [TestFixture]
    public class UserRoomConnectionTests
    {
        [Test]
        public void UserRoomConnection_Properties_AreSetCorrectly()
        {
            var user = "John";
            var room = "Lobby";

            var connection = new UserRoomConnection
            {
                User = user,
                Room = room
            };

            Assert.AreEqual(user, connection.User);
            Assert.AreEqual(room, connection.Room);
        }

        [Test]
        public void UserRoomConnection_Properties_CanBeNull()
        {
            var connection = new UserRoomConnection();

            Assert.IsNull(connection.User);
            Assert.IsNull(connection.Room);
        }
    }
}