using System.ComponentModel.DataAnnotations;
using System.Reflection.Metadata;

namespace Server.Requests;

public record AvatarChange([Required]string UserId, [Required]string Image);