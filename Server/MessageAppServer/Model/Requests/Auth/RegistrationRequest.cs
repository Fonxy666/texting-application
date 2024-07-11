using System.ComponentModel.DataAnnotations;
using Microsoft.IdentityModel.Tokens;

namespace Server.Model.Requests.Auth;

public record RegistrationRequest(
    [Required(ErrorMessage = "E-mail cannot be null.")] [EmailAddress(ErrorMessage = "The provided string is not an e-mail.")]string Email,
    [Required(ErrorMessage = "Username cannot be null.")]string Username,
    [Required(ErrorMessage = "Password cannot be null.")]string Password,
    string Image,
    [Required(ErrorMessage = "Phone number cannot be null.")]string PhoneNumber,
    [Required(ErrorMessage = "Public key cannot be null.")]string PublicKey,
    [Required(ErrorMessage = "Private key cannot be null.")]string EncryptedPrivateKey,
    [Required(ErrorMessage = "Iv cannot be null.")]string Iv
    );