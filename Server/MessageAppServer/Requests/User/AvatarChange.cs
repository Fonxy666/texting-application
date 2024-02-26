using System.ComponentModel.DataAnnotations;

namespace Server.Requests.User;

public record AvatarChange([Required]string UserId, [Required]string Image);