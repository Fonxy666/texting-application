using System.ComponentModel.DataAnnotations;

namespace Server.Requests;

public record RegistrationRequest([Required]string Email, [Required]string Username, [Required]string Password, [Required]string PhoneNumber, string Image);