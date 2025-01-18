namespace AuthenticationServer.Model.Responses.User;

public record PrivateKeyResponse(string EncryptedPrivateKey, string Iv);