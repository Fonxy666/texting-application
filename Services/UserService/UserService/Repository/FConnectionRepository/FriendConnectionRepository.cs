using Microsoft.EntityFrameworkCore;
using UserService.Database;
using UserService.Models;
using UserService.Models.Responses;

namespace UserService.Repository.FConnectionRepository;

public class FriendConnectionRepository(MainDatabaseContext context) : IFriendConnectionRepository
{
    public async Task<ReceivedAndSentFriendRequestsDto?> GetAllPendingRequestsAsync(Guid userId)
    {
        var result = await context.Users
            .Where(u => u.Id == userId)
            .Select(u => new ReceivedAndSentFriendRequestsDto(
                u.ReceivedFriendRequests
                    .Where(rfr => rfr.Status == FriendStatus.Pending)
                    .Select(rfr => new ShowFriendRequestDto(
                        rfr.ConnectionId,
                        rfr.Sender!.UserName!,
                        rfr.Sender!.Id,
                        rfr.SentTime,
                        rfr.Receiver!.UserName!,
                        rfr.Receiver.Id
                    ))
                    .ToList(),

                u.SentFriendRequests
                    .Where(rfr => rfr.Status == FriendStatus.Pending)
                    .Select(rfr => new ShowFriendRequestDto(
                        rfr.ConnectionId,
                        rfr.Sender!.UserName!,
                        rfr.Sender!.Id,
                        rfr.SentTime,
                        rfr.Receiver!.UserName!,
                        rfr.Receiver.Id
                    ))
                    .ToList()
            ))
            .FirstOrDefaultAsync();

        return result;
    }

    public async Task<int> GetPendingRequestsCountAsync(Guid userId)
    {
        return await context.FriendConnections
            .Where(rfr => rfr.ReceiverId == userId && rfr.Status == FriendStatus.Pending)
            .CountAsync();
    }

    public async Task<bool> IsFriendRequestAlreadySentAsync(Guid receiverId, Guid senderId)
    {
        return await context.Users
            .AnyAsync(u => u.ReceivedFriendRequests
                .Any(fc => fc.ReceiverId == receiverId && fc.SenderId == senderId));
    }

    public async Task<IList<ShowFriendRequestDto>?> GetFriendsAsync(Guid userId)
    {
        var friends = await context.Users
            .Where(u => u.Id == userId)
            .Select(u => context.FriendConnections
                .Where(fr => (fr.ReceiverId == u.Id || fr.SenderId == u.Id) && fr.Status == FriendStatus.Accepted)
                .Select(fr => new ShowFriendRequestDto(
                    fr.ConnectionId,
                    fr.Receiver!.UserName!,
                    fr.ReceiverId,
                    fr.AcceptedTime,
                    fr.Sender!.UserName!,
                    fr.SenderId))
                .ToList()
            )
            .SingleOrDefaultAsync();

        return friends;
    }

    public async Task<ConnectionIdAndAcceptedTimeDto?> GetConnectionIdAndAcceptedTimeAsync(Guid userId, Guid friendId)
    {
        return await context.FriendConnections
            .Where(fc => fc.ReceiverId == userId && fc.SenderId == friendId ||  fc.ReceiverId == friendId && fc.SenderId == userId)
            .Select(fc => new ConnectionIdAndAcceptedTimeDto(fc.ConnectionId, fc.AcceptedTime!.Value))
            .SingleOrDefaultAsync();
    }

    public async Task<FriendConnection?> GetFriendConnectionWithSenderAndReceiverAsync(Guid senderId, Guid  connectionId)
    {
        var connection = await context.FriendConnections
            .Include(fc => fc.Sender)
            .ThenInclude(u => u.Friends)
            .Include(fc => fc.Receiver)
            .ThenInclude(u => u.Friends)
            .FirstOrDefaultAsync(fc => fc.ConnectionId == connectionId);

        if (connection is null)
        {
            return null;
        }
        
        await context.Entry(connection.Sender)
            .Collection(u => u!.Friends)
            .Query()
            .Where(f => f.Id == connection.ReceiverId)
            .AsSingleQuery()
            .LoadAsync();
        
        await context.Entry(connection.Receiver)
            .Collection(u => u!.Friends)
            .Query()
            .Where(f => f.Id == connection.SenderId)
            .AsSingleQuery()
            .LoadAsync();

        return connection;
    }

    public async Task<ConnectionIdAndSentTimeDto?> AddFriendConnectionAsync(FriendConnection friendConnection)
    {
        var newFriendConnection = await context.FriendConnections.AddAsync(friendConnection);
        var result =  await context.SaveChangesAsync() > 0;
        if (!result)
        {
            return null;
        }
        
        return new ConnectionIdAndSentTimeDto(newFriendConnection.Entity.ConnectionId, newFriendConnection.Entity.SentTime);
    }

    public async Task<FriendConnection?> GetFriendConnectionAsync(Guid connectionId)
    {
        return await context.FriendConnections.FindAsync(connectionId);
    }

    public async Task<bool> RemoveFriendConnectionAsync(FriendConnection connection)
    {
        context.FriendConnections.Remove(connection);
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<IList<FriendConnection>> GetSentRequestsAsync(Guid userId)
    {
        return await context.FriendConnections
            .Where(fc => fc.SenderId == userId)
            .ToListAsync();
    }

    public async Task<IList<FriendConnection>> GetReceivedRequestsAsync(Guid userId)
    {
        return await context.FriendConnections
            .Where(fc => fc.ReceiverId == userId)
            .ToListAsync();
    }

    public void RemoveFriendRangeWithOutSaveChangesAsync(IList<FriendConnection> friendConnections)
    {
        context.FriendConnections.RemoveRange(friendConnections);
    }
}
    