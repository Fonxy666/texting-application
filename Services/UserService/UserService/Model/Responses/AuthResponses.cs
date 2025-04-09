namespace UserService.Model.Responses;

public abstract record AuthResponse<T>(bool IsSuccess, T? Data = default);
public struct Unit { };

public record AuthResponseSuccess(string Id) : AuthResponse<string>(true, Id);

public record SuccessfulAuthResult(string Id, string Email) : AuthResponse<(string, string)>(true, (Id, Email));

public record FailedAuthResult() : AuthResponse<Unit>(false);

public record LoginResponseSuccess(string PublicKey, string EncryptedPrivateKey) : AuthResponse<(string, string)>(true, (PublicKey, EncryptedPrivateKey));

public record LoginResponseFailure() : AuthResponse<Unit>(false);
