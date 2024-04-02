using System.ComponentModel.DataAnnotations;

namespace Server.Model.Requests.User;

public record AvatarChange([Required]string UserId, [Required]string Image);