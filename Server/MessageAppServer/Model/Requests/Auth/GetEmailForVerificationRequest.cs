using System.ComponentModel.DataAnnotations;

namespace Server.Model.Requests.Auth;

public record GetEmailForVerificationRequest([Required] string Email);