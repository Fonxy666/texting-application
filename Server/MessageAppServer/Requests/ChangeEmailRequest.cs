using System.ComponentModel.DataAnnotations;

namespace Server.Requests;

public record ChangeEmailRequest([Required]string OldEmail, [Required]string NewEmail, [Required]string Token);