using System.ComponentModel.DataAnnotations;

namespace UserService.Model.Requests.Auth;

public record GetEmailForVerificationRequest(
    [Required(ErrorMessage = "E-mail cannot be null.")]
    [EmailAddress(ErrorMessage = "The provided string is not an e-mail.")] string Email);