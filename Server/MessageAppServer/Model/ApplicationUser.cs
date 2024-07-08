using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace Server.Model;

public class ApplicationUser : IdentityUser<Guid>
{
    public string? ImageUrl { get; private set; }
    public string? RefreshToken { get; private set; } = string.Empty;
    public DateTime? RefreshTokenCreated { get; private set; }
    public DateTime? RefreshTokenExpires { get; private set; }
    public string PublicKey { get; set; }
    public ICollection<FriendConnection> SentFriendRequests { get; set; } = new List<FriendConnection>();
    public ICollection<FriendConnection> ReceivedFriendRequests { get; set; } = new List<FriendConnection>();
    public ICollection<ApplicationUser> Friends { get; set; } = new List<ApplicationUser>();

    public ApplicationUser(string? imageUrl = "-")
    {
        ImageUrl = imageUrl;
    }

    public void SetRefreshToken(string? token)
    {
        RefreshToken = token;
    }

    public void SetRefreshTokenCreated(DateTime? time)
    {
        RefreshTokenCreated = time;
    }
    
    public void SetRefreshTokenExpires(DateTime? time)
    {
        RefreshTokenExpires = time;
    }

    public void SetPublicKey(JsonWebKey key)
    {
        PublicKey = JsonConvert.SerializeObject(key);
    }
    
    public JsonWebKey GetPublicKey()
    {
        return JsonConvert.DeserializeObject<JsonWebKey>(PublicKey)!;
    }
}