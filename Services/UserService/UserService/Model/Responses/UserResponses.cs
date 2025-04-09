namespace UserService.Model.Responses;

public abstract record UserResponse<T>(bool IsSuccess, T? Data = default);

// Shared models
public record FriendHubFriendData(string RequestId, string SenderName, string SenderId, string SentTime, string ReceiverName, string ReceiverId);
public record ShowFriendRequestData(Guid RequestId, string SenderName, string SenderId, DateTime? SentTime, string ReceiverName, string ReceiverId);
public struct Unit { };

// Success/Failure responses
public record DeleteUserSuccess(string Username, string Message) : UserResponse<string>(true, Message);
public record DeleteUserFailure() : UserResponse<Unit>(false);

public record EmailUsernameResponseSuccess(string Email, string UserName) : UserResponse<(string, string)>(true, (Email, UserName));
public record EmailUsernameResponseFailure() : UserResponse<Unit>(false);

public record ForgotPasswordSuccess(string Message) : UserResponse<string>(true, Message);
public record ForgotPasswordFailure() : UserResponse<Unit>(false);

public record FriendHubFriendSuccess(FriendHubFriendData Data) : UserResponse<FriendHubFriendData>(true, Data);
public record FriendHubFriendFailure() : UserResponse<Unit>(false);

public record PrivateKeyResponseSuccess(string EncryptedPrivateKey, string Iv) : UserResponse<(string, string)>(true, (EncryptedPrivateKey, Iv));
public record PrivateKeyResponseFailure() : UserResponse<Unit>(false);

public record ShowFriendRequestResponseSuccess(ShowFriendRequestData Data) : UserResponse<ShowFriendRequestData>(true, Data);
public record ShowFriendRequestResponseFailure() : UserResponse<Unit>(false);

public record UsernameResponseSuccess(string Username) : UserResponse<string>(true, Username);
public record UsernameResponseFailure() : UserResponse<Unit>(false);

public record UserResponseForWsSuccess(string UserId, string ConnectionId) : UserResponse<(string, string)>(true, (UserId, ConnectionId));
public record UserResponseForWsFailure() : UserResponse<Unit>(false);