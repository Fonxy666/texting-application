namespace MessagesServer.Model.Responses.Message;

public record SaveMessageResponse(bool Success, Model.Message? Message, string? errorMessage);