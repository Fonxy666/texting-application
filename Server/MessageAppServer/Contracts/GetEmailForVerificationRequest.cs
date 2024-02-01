using System.ComponentModel.DataAnnotations;

namespace Server.Contracts;

public record GetEmailForVerificationRequest([Required] string Email);