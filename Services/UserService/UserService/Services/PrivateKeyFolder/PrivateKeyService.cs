using System.Text;
using System.Text.Json;
using UserService.Model;
using UserService.Model.Responses.User;

namespace UserService.Services.PrivateKeyFolder;

public class PrivateKeyService : IPrivateKeyService
{
    private readonly HttpClient _httpClient = new HttpClient();
    private readonly string _vaultToken;
    private readonly string _vaultAddress;

    public PrivateKeyService(IConfiguration configuration)
    {
        _vaultToken = configuration["HashiCorpToken"] ?? throw new Exception("Vault token missing!");
        _vaultAddress = configuration["HashiCorpAddress"] ?? throw new Exception("Vault address missing!");
    }

    public async Task<PrivateKeyResponse> GetEncryptedKeyByUserIdAsync(Guid userId)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_vaultAddress}/v1/kv/data/private_keys/{userId}");
            request.Headers.Add("X-Vault-Token", _vaultToken);

            var response = await _httpClient.SendAsync(request);
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
                return new PrivateKeyResponse(endToEndEncryptedPrivateKey, iv);
            }
            else
            {
                Console.WriteLine("Key not found in the Vault response.");
                return null;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error retrieving key: {e.Message}");
            return null;
        }
    }

    public async Task<bool> SaveKeyAsync(PrivateKey key, Guid userId)
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

            using var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Vault] Failed to save key: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteKey(Guid userId)
    {
        try
        {
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error deleting key: {e.Message}");
            return false;
        }
    }
}