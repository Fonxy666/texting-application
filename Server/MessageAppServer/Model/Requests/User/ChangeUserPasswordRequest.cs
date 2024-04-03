using System.ComponentModel.DataAnnotations;

namespace Server.Model.Requests.User;

public record ChangeUserPasswordRequest([Required]string Id, [Required]string OldPassword, [Required]string Password, [Required]string PasswordRepeat);