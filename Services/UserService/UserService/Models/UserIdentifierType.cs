namespace UserService.Models;

public enum UserIdentifierType
{
    Username,
    UsernameExamineSymmetricKeys,
    UserId,
    UserEmail,
    UserIdIncludeReceivedRequest,
    UserIdIncludeSentRequest,
    UserIdIncludeFriends,
    UserIdIncludeSymmetricKeys
}