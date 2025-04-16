namespace UserService.Models.Responses;

public abstract record AuthResponse<T>(bool IsSuccess, T? Data = default) : ResponseBase(IsSuccess);

public record AuthResponseSuccess() : AuthResponse<string>(true);

public record AuthResponseSuccessWithId(string Id) : AuthResponse<string>(true, Id);

public record AuthResponseSuccessWithMessage(string Message) : AuthResponse<string>(true, Message);

public record AuthResponseWithEmailSuccess(string Id, string Email) : AuthResponse<(string, string)>(true, (Id, Email));

public record FailedAuthResult() : AuthResponse<string>(false);

public record FailedAuthResultWithMessage(string Message) : AuthResponse<string>(false, Message);

public record LoginResponseSuccess(string PublicKey, string EncryptedPrivateKey) : AuthResponse<(string, string)>(true, (PublicKey, EncryptedPrivateKey));

public record LoginResponseFailure(string? ErrorMessage) : AuthResponse<string?>(false, ErrorMessage);
