using System.ComponentModel.DataAnnotations;

namespace Server.Model.Requests.User;

public record ChangeEmailRequest([Required]string OldEmail, [Required]string NewEmail);