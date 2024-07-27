namespace Server.Model.Requests;

public record PrivateKeyResponse(string EncryptedPrivateKey, string Iv);