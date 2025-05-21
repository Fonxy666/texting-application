using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserService.Models.Requests;
using UserService.Services.User;
using UserService.Models;
using UserService.Models.Responses;
using Textinger.Shared.Responses;
using UserService.Helpers;

namespace UserService.Services.FriendConnectionService;

public class FriendConnectionService(
    MainDatabaseContext context,
    IApplicationUserService userServices,
    UserManager<ApplicationUser> userManager,
    IUserHelper userHelper) : IFriendConnectionService
{
    public async Task<ResponseBase> SendFriendRequestAsync(Guid userId, string friendName)
    {
        var existingUserName = await context.Users
            .Where(u => u.Id == userId)
            .Select(u => u.UserName)
            .FirstOrDefaultAsync();
        
        var existingNewFriendId = await context.Users
            .Where(u => u.UserName == friendName)
            .Select(u => u.Id)
            .FirstOrDefaultAsync();
        
        if (existingNewFriendId == Guid.Empty)
        {
            return new FailureWithMessage($"There is no User with this username: {friendName}");
        }
        
        if (friendName == existingUserName)
        {
            return new Failure();
        }
        
        if (await AlreadySentFriendRequest(new FriendRequest(userId.ToString(), existingNewFriendId.ToString())))
        {
            return new FailureWithMessage("You already sent a friend request to this user!");
        }
        
        var friendRequest = new FriendConnection(userId, existingNewFriendId);

        var savedRequest = await context.FriendConnections.AddAsync(friendRequest);
        var affectedRows = await context.SaveChangesAsync();
        if (affectedRows == 0)
        {
            return new FailureWithMessage("Failed to save changes.");
        }

        return new SuccessWithDto<ShowFriendRequestDto>(new ShowFriendRequestDto(
            savedRequest.Entity.ConnectionId,
            existingUserName!,
            userId,
            savedRequest.Entity.SentTime,
            friendName,
            existingNewFriendId));
    }

    public async Task<ResponseBase> GetAllPendingRequestsAsync(Guid userId)
    {
        var rawFriendConnection = await context.Users
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                Received = u.ReceivedFriendRequests
                    .Where(rfr => rfr.Status == FriendStatus.Pending)
                    .Select(rfr => new
                    {
                        rfr.ConnectionId,
                        SenderId = rfr.Sender!.Id,
                        SenderUserName = rfr.Sender!.UserName!,
                        rfr.SentTime,
                        ReceiverUserName = rfr.Receiver!.UserName!,
                        ReceiverId = rfr.Receiver.Id
                    }),
                Sent = u.SentFriendRequests
                    .Where(rfr => rfr.Status == FriendStatus.Pending)
                    .Select(rfr => new
                    {
                        rfr.ConnectionId,
                        SenderId = rfr.Sender!.Id,
                        SenderUserName = rfr.Sender!.UserName!,
                        rfr.SentTime,
                        ReceiverUserName = rfr.Receiver!.UserName!,
                        ReceiverId = rfr.Receiver.Id
                    })
            })
            .FirstOrDefaultAsync();
        
        if (rawFriendConnection == null)
        {
            return new FailureWithMessage("User not found.");
        }
        
        var friendRequestsDto = rawFriendConnection.Received
            .Concat(rawFriendConnection.Sent)
            .Select(rfr => new ShowFriendRequestDto(
                rfr.ConnectionId,
                rfr.SenderUserName,
                rfr.SenderId,
                rfr.SentTime,
                rfr.ReceiverUserName,
                rfr.ReceiverId
            ))
            .ToList();
        

        return new SuccessWithDto<List<ShowFriendRequestDto>>(friendRequestsDto);
    }
    
    public async Task<ResponseBase> GetPendingRequestCountAsync(Guid userId)
    {
        var pendingRequestCount = await context.FriendConnections
            .Where(rfr => rfr.ReceiverId == userId && rfr.Status == FriendStatus.Pending)
            .CountAsync();
        
        return new SuccessWithDto<NumberDto>(new NumberDto(pendingRequestCount));
    }

    private async Task<bool> AlreadySentFriendRequest(FriendRequest request)
    {
        if (!Guid.TryParse(request.Receiver, out var receiverGuid) || !Guid.TryParse(request.SenderId, out var senderGuid))
        {
            throw new ArgumentException("Invalid Receiver or SenderId format.");
        }

        return await context.Users
            .AnyAsync(u => u.ReceivedFriendRequests
                .Any(fc => fc.ReceiverId == receiverGuid && fc.SenderId == senderGuid));
    }

    public async Task<ResponseBase> AcceptReceivedFriendRequestAsync(Guid userId, Guid requestId)
    {
        var existingRequest = await context.FriendConnections!.FindAsync(requestId);
        if (existingRequest == null)
        {
            return new FailureWithMessage("Request not found.");
        }

        await using var transaction = await context.Database.BeginTransactionAsync();

        existingRequest.SetStatusToAccepted();

        var linkResult = await LinkUsersAsFriendsAsync(userId, existingRequest.SenderId);
        if (linkResult is Failure)
        {
            await transaction.RollbackAsync();
            return linkResult;
        }

        var affectedRows = await context.SaveChangesAsync();
        if (affectedRows == 0)
        {
            await transaction.RollbackAsync();
            return new FailureWithMessage("Failed to persist friend request acceptance.");
        }

        await transaction.CommitAsync();
        return new Success();
    }

    private async Task<ResponseBase> LinkUsersAsFriendsAsync(Guid receiverId, Guid senderId)
    {
        var user = await context.Users
            .Include(u => u.Friends).FirstOrDefaultAsync(u => u.Id == receiverId);
        
        var friend = await context.Users
            .Include(u => u.Friends).FirstOrDefaultAsync(u => u.Id == senderId);

        if (user == null || friend == null)
        {
            return new FailureWithMessage("User or sender not found.");
        }

        user.Friends.Add(friend);
        friend.Friends.Add(user);

        return new Success();
    }

    public async Task<ResponseBase> DeleteFriendRequestAsync(Guid userId, UserType userType, Guid requestId)
    {
        return userType switch
        {
            UserType.Receiver => await DeleteReceivedFriendRequestAsync(requestId, userId!),
            UserType.Sender => await DeleteSentFriendRequestAsync(requestId, userId!),
            _ => new FailureWithMessage("Invalid user type.")
        };
    }

    private async Task<ResponseBase> DeleteSentFriendRequestAsync(Guid requestId, Guid senderId)
    {
        var user = await userServices.GetUserWithSentRequestsAsync(senderId);

        var request = (user as SuccessWithDto<ApplicationUser>)!.Data!.SentFriendRequests.FirstOrDefault(fc => fc.ConnectionId == requestId);

        if (request == null)
        {
            return new FailureWithMessage("Cannot find the request.");
        }
        
        context.FriendConnections!.Remove(request);
        var affectedRows = await context.SaveChangesAsync();
        if (affectedRows == 0)
        {
            return new FailureWithMessage("Failed to update connections.");
        }

        return new Success();
    }

    private async Task<ResponseBase> DeleteReceivedFriendRequestAsync(Guid requestId, Guid receiverId)
    {
        var user = await userServices.GetUserWithReceivedRequestsAsync(receiverId);

        var request = (user as SuccessWithDto<ApplicationUser>)!.Data!.ReceivedFriendRequests.FirstOrDefault(fc => fc.ConnectionId == requestId);

        if (request == null)
        {
            return new FailureWithMessage("Cannot find the request.");
        }

        context.FriendConnections!.Remove(request);
        var affectedRows = await context.SaveChangesAsync();
        if (affectedRows == 0)
        {
            return new FailureWithMessage("Failed to update connections.");
        }

        return new Success();
    }

    public async Task<ResponseBase> GetFriendsAsync(Guid userId)
    {
        var returningDto = await context.FriendConnections
            .Where(fr => 
                (fr.Receiver!.Id == userId || fr.Sender!.Id == userId)
                && fr.Status == FriendStatus.Accepted)
            .Select(fr => new ShowFriendRequestDto(
                fr.ConnectionId,
                fr.Receiver!.UserName!,
                fr.ReceiverId,
                fr.AcceptedTime,
                fr.Sender!.UserName!,
                fr.SenderId
                ))
            .ToArrayAsync();
        Console.WriteLine(returningDto.Length);

        return new SuccessWithDto<ShowFriendRequestDto[]>(returningDto);
    }

    public async Task<FriendConnection?> GetConnectionIdAsync(Guid userId, Guid friendId)
    {
        return await context.FriendConnections!.FirstOrDefaultAsync(fc =>
            fc.ReceiverId == userId && fc.SenderId == friendId || fc.ReceiverId == friendId && fc.SenderId == userId);
    }

    public async Task<ResponseBase> DeleteFriendAsync(Guid userId, Guid connectionId)
    {
        var connection = await context.FriendConnections
            .Include(fc => fc.Sender)
                .ThenInclude(u => u.Friends)
            .Include(fc => fc.Receiver)
                .ThenInclude(u => u.Friends)
            .FirstOrDefaultAsync(fc => fc.ConnectionId == connectionId);
        
        if (connection == null)
        {
            return new FailureWithMessage("Cannot find friend connection.");
        }
        
        await context.Entry(connection.Sender)
            .Collection(u => u.Friends)
            .Query()
            .Where(f => f.Id == connection.ReceiverId)
            .AsSingleQuery()
            .LoadAsync();
        
        await context.Entry(connection.Receiver)
            .Collection(u => u.Friends)
            .Query()
            .Where(f => f.Id == connection.SenderId)
            .AsSingleQuery()
            .LoadAsync();

       if (userId != connection.Sender.Id && userId != connection.Receiver.Id)
       {
           return new FailureWithMessage("You don't have permission for deletion.");
       }

       await using var transaction = await context.Database.BeginTransactionAsync();

        context.FriendConnections.Remove(connection);

        var unlinkResult = UnlinkFriendsAsync(connection.Sender, connection.Receiver);
        if (unlinkResult is FailureWithMessage)
        {
            await transaction.RollbackAsync();
            return unlinkResult;
        }

        var affectedRows = await context.SaveChangesAsync();
        if (affectedRows == 0)
        {
            await transaction.RollbackAsync();
            return new FailureWithMessage("Failed to update connections.");
        }

        await transaction.CommitAsync();
        return new Success();
    }

    private ResponseBase UnlinkFriendsAsync(ApplicationUser sender, ApplicationUser receiver)
    {
        sender.Friends.Remove(receiver);
        receiver.Friends.Remove(sender);

        return new Success();
    }
}