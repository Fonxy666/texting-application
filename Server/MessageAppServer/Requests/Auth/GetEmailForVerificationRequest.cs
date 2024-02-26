using System.ComponentModel.DataAnnotations;

namespace Server.Requests.Auth;

public record GetEmailForVerificationRequest([Required] string Email);