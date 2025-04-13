namespace UserService.Model.Responses;

public abstract record UserResponse<T>(bool IsSuccess, T? Data = default) : ResponseBase(IsSuccess);

// Shared models
public record FriendHubFriendData(string RequestId, string SenderName, string SenderId, string SentTime, string ReceiverName, string ReceiverId);
public record ShowFriendRequestData(Guid RequestId, string SenderName, string SenderId, DateTime? SentTime, string ReceiverName, string ReceiverId);

// Success/Failure responses
public record DeleteUserSuccess(string Username, string Message) : UserResponse<string>(true, Message);
public record DeleteUserFailure(string? ErrorMessage) : UserResponse<string>(false, ErrorMessage);

public record EmailUsernameResponseSuccess(string Email, string UserName) : UserResponse<(string, string)>(true, (Email, UserName));
public record EmailUsernameResponseFailure(string? ErrorMessage) : UserResponse<string?>(false);

public record ForgotPasswordSuccess(string Message) : UserResponse<string>(true, Message);
public record ForgotPasswordFailure(string? ErrorMessage) : UserResponse<string?>(false, ErrorMessage);

public record FriendHubFriendSuccess(FriendHubFriendData Data) : UserResponse<FriendHubFriendData>(true, Data);
public record FriendHubFriendFailure(string? ErrorMessage) : UserResponse<string?>(false, ErrorMessage);

public record PrivateKeyResponseSuccess(string EncryptedPrivateKey, string Iv) : UserResponse<(string, string)>(true, (EncryptedPrivateKey, Iv));
public record PrivateKeyResponseFailure(string? ErrorMessage) : UserResponse<string?>(false, ErrorMessage);

public record ShowFriendRequestResponseSuccess(ShowFriendRequestData Data) : UserResponse<ShowFriendRequestData>(true, Data);
public record ShowFriendRequestResponseFailure(string? ErrorMessage) : UserResponse<string?>(false, ErrorMessage);

public record UsernameResponseSuccess(string Username) : UserResponse<string>(true, Username);
public record UsernameResponseFailure(string? ErrorMessage) : UserResponse<string?>(false, ErrorMessage);

public record UserResponseForWsSuccess(string UserId, string ConnectionId) : UserResponse<(string, string)>(true, (UserId, ConnectionId));
public record UserResponseForWsFailure(string? ErrorMessage) : UserResponse<string?>(false, ErrorMessage);