namespace UserService.Models.Responses;

public record ShowFriendRequestDto(Guid RequestId, string SenderName, string SenderId, DateTime? SentTime, string ReceiverName, string ReceiverId);
public record UsernameUserEmailAndTwoFactorEnabledDto(string Username, string Email, bool TwoFactorEnabled);
public record KeyAndIvDto(string EncryptedPrivateKey, string Iv);
public record ImageDto(byte[] ImageBytes, string ContentType);
public record UserNameEmailDto(string Username, string Email);
public record UserEmailDto(string Email);
public record UserNameDto(string UserName);
public record ConnectionDto(string UserId, string ConnectionId);
public record UserPrivateKeyDto(string EncryptedPrivateKey);
public record UserPublicKeyDto(string PublicKey);
public record UserIdAndEmailDto(string Id, string Email);
public record UserIdDto(string Id);
public record UserIdAndConnectionIdDto(string UserId, string ConnectionId);
public record NumberDto(int Count);
public record KeysDto(string PubblicKey, string PrivateKey);