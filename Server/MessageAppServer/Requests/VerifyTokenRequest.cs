using System.ComponentModel.DataAnnotations;

namespace Server.Requests;

public record VerifyTokenRequest([Required] string Email, string VerifyCode);