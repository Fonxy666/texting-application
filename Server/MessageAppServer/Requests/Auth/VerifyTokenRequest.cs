using System.ComponentModel.DataAnnotations;

namespace Server.Requests.Auth;

public record VerifyTokenRequest([Required] string Email, string VerifyCode);