using System.ComponentModel.DataAnnotations;

namespace Server.Model.Requests.Auth;

public record RegistrationRequest(
    [Required(ErrorMessage = "E-mail cannot be null.")] [EmailAddress(ErrorMessage = "The provided string is not an e-mail.")]string Email,
    [Required(ErrorMessage = "Username cannot be null.")]string Username,
    [Required(ErrorMessage = "Password cannot be null.")]string Password,
    [Required(ErrorMessage = "Phone number cannot be null.")]string PhoneNumber,
    string Image);