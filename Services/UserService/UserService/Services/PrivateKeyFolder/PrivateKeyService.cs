using System.Text;
using System.Text.Json;
using UserService.Models;
using UserService.Models.Responses;
using Textinger.Shared.Responses;

namespace UserService.Services.PrivateKeyFolder;

public class PrivateKeyService(HttpClient httpClient, string vaultToken, string vaultAddress)
    : IPrivateKeyService
{
    private readonly string _vaultToken = vaultToken ?? throw new ArgumentNullException(nameof(vaultToken));
    private readonly string _vaultAddress = vaultAddress ?? throw new ArgumentNullException(nameof(vaultAddress));

    public async Task<ResponseBase> GetEncryptedKeyByUserIdAsync(Guid userId)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_vaultAddress}/v1/kv/data/private_keys/{userId}");
            request.Headers.Add("X-Vault-Token", _vaultToken);

            var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadAsStringAsync();

            var vaultResponse = JsonSerializer.Deserialize<JsonElement>(jsonResponse);

            var privateKey = vaultResponse
                .GetProperty("data")
                .GetProperty("data")
                .GetProperty("private_key");

            var endToEndEncryptedPrivateKey = privateKey.GetProperty("EndToEndEncryptedPrivateKey").GetString();
            var iv = privateKey.GetProperty("Iv").GetString();

            if (endToEndEncryptedPrivateKey != null && iv != null)
            {
                return new SuccessWithDto<KeyAndIvDto>(new KeyAndIvDto(endToEndEncryptedPrivateKey, iv));
            }
            
            Console.WriteLine("Key not found in the Vault response.");
            return new Failure();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error retrieving key: {e.Message}");
            return new Failure();
        }
    }

    public async Task<ResponseBase> SaveKeyAsync(PrivateKey key, Guid userId)
    {
        try
        {
            var payload = new
            {
                data = new
                {
                    private_key = key,
                    metadata = new
                    {
                        created_by = "admin",
                        created_at = DateTime.UtcNow.ToString("o")
                    }
                }
            };

            var jsonPayload = JsonSerializer.Serialize(payload);
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_vaultAddress}/v1/kv/data/private_keys/{userId}")
            {
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };

            request.Headers.Add("X-Vault-Token", _vaultToken);

            using var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return new Success();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Vault] Failed to save key: {ex.Message}");
            return new Failure();
        }
    }

    public async Task<ResponseBase> DeleteKey(Guid userId)
    {
        try
        {
            return new Success();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error deleting key: {e.Message}");
            return new Failure();
        }
    }
}
