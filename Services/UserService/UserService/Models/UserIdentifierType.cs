namespace UserService.Models;

public enum UserIdentifierType
{
    Username,
    UsernameExamineSymmetricKeys,
    UserId,
    UserEmail,
    UserIdIncludeReceivedRequests,
    UserIdIncludeReceiverAndSentRequests,
    UserIdIncludeSentRequests,
    UserIdIncludeFriends,
    UserIdIncludeSentRequestsAndReceivers,
    UserIdIncludeReceivedRequestsAndSenders,
    UserIdIncludeSymmetricKeys
}