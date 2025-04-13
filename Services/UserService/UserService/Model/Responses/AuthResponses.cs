namespace UserService.Model.Responses;

public abstract record AuthResponse<T>(bool IsSuccess, T? Data = default) : ResponseBase(IsSuccess);

public record AuthResponseSuccess(string? Id) : AuthResponse<string>(true, Id);

public record AuthResponseWithEmailSuccess(string Id, string Email) : AuthResponse<(string, string)>(true, (Id, Email));

public record FailedAuthResult(string? ErrorMessage) : AuthResponse<string?>(false, ErrorMessage);

public record LoginResponseSuccess(string PublicKey, string EncryptedPrivateKey) : AuthResponse<(string, string)>(true, (PublicKey, EncryptedPrivateKey));

public record LoginResponseFailure(string? ErrorMessage) : AuthResponse<string?>(false, ErrorMessage);
