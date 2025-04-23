namespace UserService.Models.Responses;

public abstract record AuthResponse<T>(bool IsSuccess, T? Data = default) : ResponseBase(IsSuccess);
// Shared models
public record UserIdAndEmailData(string Id, string Email);
public record KeyData(string UserId, string ConnectionId);

// Success/Failure responses

public record AuthResponseSuccess() : AuthResponse<string>(true);

public record AuthResponseSuccessWithId(string Id) : AuthResponse<string>(true, Id);

public record AuthResponseSuccessWithMessage(string Message) : AuthResponse<string>(true, Message);

public record AuthResponseWithEmailSuccess(UserIdAndEmailData Data) : AuthResponse<UserIdAndEmailData>(true, Data);

public record LoginResponseSuccess(KeyData Data) : AuthResponse<KeyData>(true, Data);