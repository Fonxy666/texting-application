using System.ComponentModel.DataAnnotations;

namespace Server.Requests;

public record GetEmailForVerificationRequest([Required] string Email);