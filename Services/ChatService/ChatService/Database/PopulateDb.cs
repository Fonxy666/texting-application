using ChatService.Model;
using ChatService.Repository.RoomRepository;

namespace ChatService.Database;

public static class PopulateDb
{
    private static readonly object LockObject = new();
    
    public static void CreateTestRooms(IServiceProvider services, int numberOfTestRooms)
    {
        lock (LockObject)
        {
            using var scope = services.CreateScope();
            var roomRepository = scope.ServiceProvider.GetRequiredService<IRoomRepository>();

            var userGuid = Guid.Parse("b8c1e8e9-3c3f-4d60-9c2d-5e8f6a2a9d5e");

            for (var i = 1; i <= numberOfTestRooms; i++)
            {
                var roomRequest = new Room($"testRoomName{i}", "testRoomPassword", userGuid);
                roomRepository.AddRoomAsync(roomRequest);
            }
        }
    }
}