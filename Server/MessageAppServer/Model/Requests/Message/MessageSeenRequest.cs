using System.ComponentModel.DataAnnotations;

namespace Server.Model.Requests.Message;

public record MessageSeenRequest([Required]string UserId, [Required]bool AsAnonymous, [Required]string MessageId);