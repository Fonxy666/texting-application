using System.ComponentModel.DataAnnotations;

namespace Server.Contracts;

public record ChangeEmailRequest([Required]string OldEmail, [Required]string NewEmail, [Required]string Token);