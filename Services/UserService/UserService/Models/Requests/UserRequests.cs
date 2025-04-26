using System.ComponentModel.DataAnnotations;

namespace UserService.Models.Requests;

public record ChangeEmailRequest(
    [Required(ErrorMessage = "Old e-mail cannot be null.")]
    [EmailAddress(ErrorMessage = "The provided string is not an e-mail.")]string OldEmail,
    [Required(ErrorMessage = "New e-mail cannot be null.")]
    [EmailAddress(ErrorMessage = "The provided string is not an e-mail.")]string NewEmail
);

public record ChangePasswordRequest(
    [Required(ErrorMessage = "Old password cannot be null.")] string OldPassword,
    [Required(ErrorMessage = "Password cannot be null.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*\W).+$",
        ErrorMessage = "Password must contain at least one lowercase letter, one uppercase letter, one digit, and one special character.")]
    string Password
);

public record FriendRequest(
     [Required(ErrorMessage = "SenderId cannot be null.")] string SenderId,
      [Required(ErrorMessage = "Receiver cannot be null.")] string Receiver
);

public record PasswordResetRequest(
    [Required(ErrorMessage = "E-mail cannot be null.")] string Email,
    [Required(ErrorMessage = "New password cannot be null.")] string NewPassword
);

public record StoreRoomKeyRequest(
    [Required(ErrorMessage = "EncryptedKey cannot be null.")] string EncryptedKey,
    [Required(ErrorMessage = "RoomId cannot be null.")] string RoomId
);

public record ManageFriendRequest(
    [Required(ErrorMessage = "Request id cannot be null.")] string RequestId,
    [Required(ErrorMessage = "Sender name cannot be null.")] string SenderName,
    [Required(ErrorMessage = "Sender id cannot be null.")] string SenderId,
    [Required(ErrorMessage = "Sent time cannot be null.")] string SentTime,
    [Required(ErrorMessage = "Receiver name cannot be null.")] string ReceiverName,
    [Required(ErrorMessage = "Receiver name cannot be null.")] string ReceiverId
);

public record ChatRoomInviteRequest(
    [Required(ErrorMessage = "Room id cannot be null.")] string RoomId,
    [Required(ErrorMessage = "Room name cannot be null.")] string RoomName,
    [Required(ErrorMessage = "Receiver name cannot be null.")] string ReceiverName,
    [Required(ErrorMessage = "Sender id cannot be null.")] string SenderId,
    [Required(ErrorMessage = "Sender name cannot be null.")] string SenderName,
    string? RoomKey
);

public record DeleteFriendRequest(
    [Required(ErrorMessage = "Room id cannot be null.")] string RequestId,
    [Required(ErrorMessage = "Room name cannot be null.")] string SenderId,
    [Required(ErrorMessage = "Receiver name cannot be null.")] string ReceiverId
);
