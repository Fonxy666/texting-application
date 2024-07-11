using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Server.Model.Chat;

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
    public ICollection<Room> CreatedRooms { get; set; } = new List<Room>();
    public ICollection<ApplicationUser> Friends { get; set; } = new List<ApplicationUser>();
    public ICollection<EncryptedSymmetricKey> UsersSymmetricKeys { get; set; } = new List<EncryptedSymmetricKey>();

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

    public void SetPublicKey(string key)
    {
        PublicKey = key;
    }
    
    public JsonWebKey GetPublicKey()
    {
        return JsonConvert.DeserializeObject<JsonWebKey>(PublicKey)!;
    }
}