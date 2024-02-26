using System.ComponentModel.DataAnnotations;

namespace Server.Requests.User;

public record ChangeUserPasswordRequest([Required]string Id, [Required]string OldPassword, [Required]string NewPassword);