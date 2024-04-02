using System.ComponentModel.DataAnnotations;

namespace Server.Model.Requests.User;

public record DeleteUserRequest([Required]string Email, [Required]string Username, [Required]string Password);