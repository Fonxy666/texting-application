using System.ComponentModel.DataAnnotations;

namespace Server.Model.Requests.Message;

public record EditMessageSeenRequest([Required]string messageId, [Required]string userId);