namespace UserService.Model.Responses.Auth;

public record LoginResponse(bool Success, string PublicKey, string EncryptedPrivateKey);