namespace AuthenticationService.Model.Responses.Auth;

public record FailedAuthResult(bool Success,
        string Id,
        string Email,
        string AdditionalInfo)
    : AuthResult(Success, Id, Email);