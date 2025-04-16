using System.ComponentModel.DataAnnotations;

namespace UserService.Models.Requests;

public record AuthRequest(
    [Required(ErrorMessage = "Username cannot be null.")] string UserName,
    [Required(ErrorMessage = "Password cannot be null.")] string Password);

public record GetEmailForVerificationRequest(
    [Required(ErrorMessage = "E-mail cannot be null.")]
    [EmailAddress(ErrorMessage = "The provided string is not an e-mail.")] string Email,
    [Required(ErrorMessage = "Username cannot be null.")] string Username);

public record LoginAuth(
    [Required(ErrorMessage = "Username cannot be null.")] string UserName,
    [Required(ErrorMessage = "You need to provide a 'rememberme' boolean.")] bool RememberMe,
    [Required(ErrorMessage = "Token cannot be null.")] string Token);

public record RegistrationRequest(
    [Required(ErrorMessage = "E-mail cannot be null.")][EmailAddress(ErrorMessage = "The provided string is not an e-mail.")] string Email,
    [Required(ErrorMessage = "Username cannot be null.")] string Username,
    [Required(ErrorMessage = "Password cannot be null.")] string Password,
    string Image,
    [Required(ErrorMessage = "Phone number cannot be null.")] string PhoneNumber,
    [Required(ErrorMessage = "Public key cannot be null.")] string PublicKey,
    [Required(ErrorMessage = "Private key cannot be null.")] string EncryptedPrivateKey,
    [Required(ErrorMessage = "Iv cannot be null.")] string Iv
    );

public record VerifyTokenRequest(
    [Required(ErrorMessage = "E-mail cannot be null.")] string Email,
    [Required(ErrorMessage = "Token cannot be null.")] string VerifyCode);
