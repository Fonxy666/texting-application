namespace UserService.Models.Responses;

public abstract record UserResponse<T>(bool IsSuccess, T? Data = default) : ResponseBase(IsSuccess);

// Shared models
public record FriendHubFriendData(string RequestId, string SenderName, string SenderId, string SentTime, string ReceiverName, string ReceiverId);
public record ShowFriendRequestData(Guid RequestId, string SenderName, string SenderId, DateTime? SentTime, string ReceiverName, string ReceiverId);

// Success/Failure responses
public record UserResponseSuccessWithMessage(string Message) : UserResponse<string>(true, Message);

public record FriendHubFriendSuccess(FriendHubFriendData Data) : UserResponse<FriendHubFriendData>(true, Data);

public record KeyResponseSuccess(string Key) : UserResponse<string>(true, Key);

public record ImageResponseSuccess(byte[] ImageBytes, string ContentType) : UserResponse<(byte[], string)>(true, (ImageBytes, ContentType));

public record GetUserCredentialsSuccess(string Username, string Email, bool TwoFactorEnabled) : UserResponse<(string, string, bool)>(true, (Username, Email, TwoFactorEnabled));

public record PrivateKeyResponseSuccessWithIv(string EncryptedKey, string Iv) : UserResponse<(string, string)>(true, (EncryptedKey, Iv));

public record UsernameUserEmailResponseSuccess(string Username, string Email) : UserResponse<(string, string)>(true, (Username, Email));

public record ShowFriendRequestResponseSuccess(ShowFriendRequestData Data) : UserResponse<ShowFriendRequestData>(true, Data);

public record UserResponseSuccess() : UserResponse<string>(true);

public record UserResponseForWsSuccess(string UserId, string ConnectionId) : UserResponse<(string, string)>(true, (UserId, ConnectionId));