using System.ComponentModel.DataAnnotations;

namespace Server.Requests;

public record ChangeUserPasswordRequest([Required]string Id, [Required]string OldPassword, [Required]string NewPassword);