using System.ComponentModel.DataAnnotations;

namespace MessagesServer.Model.Requests.Message;

public record EditMessageSeenRequest([Required(ErrorMessage = "Message id cannot be null.")]string MessageId);