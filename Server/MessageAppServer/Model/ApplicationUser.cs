using Microsoft.AspNetCore.Identity;

namespace AuthenticationServer.Model;

public class ApplicationUser : IdentityUser<Guid>
{
    public string? ImageUrl { get; private set; }
    public string? RefreshToken { get; private set; } = string.Empty;
    public DateTime? RefreshTokenCreated { get; private set; }
    public DateTime? RefreshTokenExpires { get; private set; }
    public string PublicKey { get; set; }
    public ICollection<FriendConnection> SentFriendRequests { get; private set; } = new List<FriendConnection>();
    public ICollection<FriendConnection> ReceivedFriendRequests { get; private set; } = new List<FriendConnection>();
    public ICollection<Guid> CreatedRoomIds { get; private set; } = new List<Guid>();
    public ICollection<ApplicationUser> Friends { get; private set; } = new List<ApplicationUser>();
    public ICollection<Guid> UserSymmetricKeyIds { get; private set; } = new List<Guid>();

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