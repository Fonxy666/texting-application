using System.ComponentModel.DataAnnotations;

namespace Server.Contracts;

public record ChangeUserPasswordRequest([Required]string Email, [Required]string OldPassword, [Required]string NewPassword);