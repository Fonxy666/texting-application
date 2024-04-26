using System.ComponentModel.DataAnnotations;

namespace Server.Model.Requests.User;

public record AvatarChange(
    [Required(ErrorMessage = "User id cannot be null.")]string UserId,
    [Required(ErrorMessage = "Image string cannot be null.")]string Image);