﻿using Server.Model;
using Server.Model.Requests.User;
using Server.Model.Responses.User;

namespace Server.Services.FriendConnection;

public interface IFriendConnectionService
{
    Task<Model.FriendConnection> GetFriendRequestByIdAsync(string requestId);
    Task<ShowFriendRequestResponse> SendFriendRequest(FriendRequest request);
    Task<IEnumerable<ShowFriendRequestResponse>> GetPendingReceivedFriendRequests(string userId);
    Task<IEnumerable<ShowFriendRequestResponse>> GetPendingSentFriendRequests(string userId);
    Task<int> GetPendingRequestCount(string userId);
    Task<bool> AlreadySentFriendRequest(FriendRequest request);
    Task<bool> AcceptReceivedFriendRequest(string requestId, string receiverId);
    Task<bool> DeleteSentFriendRequest(string requestId, string senderId);
    Task<bool> DeclineReceivedFriendRequest(string requestId, string receiverId);
    Task<IEnumerable<ShowFriendRequestResponse>> GetFriends(string userId);
}