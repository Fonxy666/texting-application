using System.ComponentModel.DataAnnotations;

namespace Server.Contracts;

public record RegistrationRequest([Required]string Email, [Required]string Username, [Required]string Password, string Image);