using System.ComponentModel.DataAnnotations;

namespace Server.Model.Requests.Message;

public record EditMessageSeenRequest([Required]string MessageId, [Required]string UserId);