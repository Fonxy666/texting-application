using System.ComponentModel.DataAnnotations;

namespace Server.Model.Requests.Auth;

public record VerifyTokenRequest([Required] string Email, string VerifyCode);