using System.ComponentModel.DataAnnotations;

namespace Server.Requests;

public record ChangeUserPasswordRequest([Required]string Email, [Required]string OldPassword, [Required]string NewPassword);