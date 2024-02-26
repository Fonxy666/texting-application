using System.ComponentModel.DataAnnotations;

namespace Server.Requests.User;

public record ChangeEmailRequest([Required]string OldEmail, [Required]string NewEmail);