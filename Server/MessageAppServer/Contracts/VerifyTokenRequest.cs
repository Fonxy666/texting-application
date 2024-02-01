using System.ComponentModel.DataAnnotations;

namespace Server.Contracts;

public record VerifyTokenRequest([Required] string Email, string VerifyCode);