namespace UserService.Models.Responses;

public abstract record UserResponse<T>(bool IsSuccess, T? Data = default) : ResponseBase(IsSuccess);

// Shared models
public record FriendHubFriendData(string RequestId, string SenderName, string SenderId, string SentTime, string ReceiverName, string ReceiverId);
public record ShowFriendRequestData(Guid RequestId, string SenderName, string SenderId, DateTime? SentTime, string ReceiverName, string ReceiverId);

// Success/Failure responses
public record DeleteUserSuccess(string Username, string Message) : UserResponse<string>(true, Message);

public record EmailUsernameResponseSuccess(string Email, string UserName) : UserResponse<(string, string)>(true, (Email, UserName));

public record ForgotPasswordSuccess(string Message) : UserResponse<string>(true, Message);

public record FriendHubFriendSuccess(FriendHubFriendData Data) : UserResponse<FriendHubFriendData>(true, Data);

public record PrivateKeyResponseSuccess(string EncryptedKey) : UserResponse<string>(true, EncryptedKey);

public record PrivateKeyResponseSuccessWithIv(string EncryptedKey, string Iv) : UserResponse<(string, string)>(true, (EncryptedKey, Iv));

public record ShowFriendRequestResponseSuccess(ShowFriendRequestData Data) : UserResponse<ShowFriendRequestData>(true, Data);

public record UsernameResponseSuccess(string Username) : UserResponse<string>(true, Username);

public record UserResponseForWsSuccess(string UserId, string ConnectionId) : UserResponse<(string, string)>(true, (UserId, ConnectionId));

public record FailedUserResponse() : UserResponse<string>(false);
public record FailedUserResponseWithMessage(string ErrorMessage) : UserResponse<string>(false, ErrorMessage);