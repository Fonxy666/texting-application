using Microsoft.AspNetCore.Identity;

namespace UserService.Model;

public class ApplicationUser : IdentityUser<Guid>
{
    public string? ImageUrl { get; private set; }
    public string? RefreshToken { get; private set; } = string.Empty;
    public DateTime? RefreshTokenCreated { get; private set; }
    public DateTime? RefreshTokenExpires { get; private set; }
    public string PublicKey { get; set; }

    public List<FriendConnection> SentFriendRequests { get; private set; } = new();
    public List<FriendConnection> ReceivedFriendRequests { get; private set; } = new();
    public List<Guid> CreatedRoomIds { get; private set; } = new();
    public List<ApplicationUser> Friends { get; private set; } = new();
    public List<Guid> UserSymmetricKeyIds { get; private set; } = new();

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

    public void AddKey(Guid keyId)
    {
        UserSymmetricKeyIds.Add(keyId);
    }
}