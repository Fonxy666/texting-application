using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.Commons;
using AuthenticationService.Model.Responses.User;

namespace AuthenticationService.Services.PrivateKey;

public class PrivateKeyService : IPrivateKeyService
{
    private readonly string _vaultToken;
    private readonly IVaultClient _vaultClient;
    private const string SecretPath = "secret/private-keys";

    public PrivateKeyService(IConfiguration configuration)
    {
        _vaultToken = configuration["HashiCorpToken"] ?? throw new Exception("Vault token missing!");

        var authMethod = new TokenAuthMethodInfo(_vaultToken);
        _vaultClient = new VaultClient(new VaultClientSettings("http://localhost:8200", authMethod));
    }

    public async Task<PrivateKeyResponse> GetEncryptedKeyByUserIdAsync(Guid userId)
    {
        try
        {
            Secret<SecretData> secret = await _vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(
                path: $"private-keys/{userId}",
                mountPoint: "secret"
            );

            var data = secret.Data.Data;
            return new PrivateKeyResponse(
                data["encryptedKey"].ToString(),
                data["iv"].ToString()
            );
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error retrieving key: {e.Message}");
            return null;
        }
    }

    public async Task<bool> SaveKey(Model.PrivateKey key, Guid userId)
    {
        try
        {
            var secretData = new Dictionary<string, object>
        {
            { "encryptedKey", key.EndToEndEncryptedPrivateKey },
            { "iv", key.Iv }
        };

            var secret = await _vaultClient.V1.Secrets.KeyValue.V2.WriteSecretAsync(
                path: $"private-keys/{userId}",
                data: secretData,
                mountPoint: "secret"
            );

            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error saving key: {e.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteKey(Guid userId)
    {
        try
        {
            await _vaultClient.V1.Secrets.KeyValue.V2.DeleteSecretAsync(
                path: $"{SecretPath}/{userId}",
                mountPoint: "secret"
            );

            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error deleting key: {e.Message}");
            return false;
        }
    }
}