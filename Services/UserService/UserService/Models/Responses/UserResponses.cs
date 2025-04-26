namespace UserService.Models.Responses;

public abstract record UserResponse<T>(bool IsSuccess, T? Data = default) : ResponseBase(IsSuccess);

// Shared models
public record FriendHubFriendData(string RequestId, string SenderName, string SenderId, DateTime? SentTime, string ReceiverName, string ReceiverId);
public record ShowFriendRequestData(Guid RequestId, string SenderName, string SenderId, DateTime? SentTime, string ReceiverName, string ReceiverId);
public record GetUserCredentialsData(string Username, string Email, bool TwoFactorEnabled);
public record KeyAndIvData(string EncryptedKey, string Iv);
public record ImageData(byte[] ImageBytes, string ContentType);
public record UserNameEmailData(string Username, string Email);
public record ConnectionData(string UserId, string ConnectionId);

// Success/Failure responses
public record UserResponseSuccessWithMessage(string Message) : UserResponse<string>(true, Message);

public record FriendHubFriendSuccess(FriendHubFriendData Data) : UserResponse<FriendHubFriendData>(true, Data);

public record KeyResponseSuccess(string Key) : UserResponse<string>(true, Key);

public record ImageResponseSuccess(ImageData Data) : UserResponse<ImageData>(true, Data);

public record GetUserCredentialsSuccess(GetUserCredentialsData Data) : UserResponse<GetUserCredentialsData>(true, Data);

public record PrivateKeyResponseSuccessWithIv(KeyAndIvData Data) : UserResponse<KeyAndIvData>(true, Data);

public record UsernameUserEmailResponseSuccess(UserNameEmailData Data) : UserResponse<UserNameEmailData>(true, Data);

public record ShowFriendRequestResponseSuccess(ShowFriendRequestData Data) : UserResponse<ShowFriendRequestData>(true, Data);

public record ShowFriendRequestsListResponseSuccess(List<ShowFriendRequestData> Data) : UserResponse<List<ShowFriendRequestData>>(true, Data);

public record UserResponseSuccess() : UserResponse<string>(true);

public record UserResponseSuccessWithNumber(int Count) : UserResponse<int>(true, Count);

public record UserResponseForWsSuccess(ConnectionData Data) : UserResponse<ConnectionData>(true, Data);