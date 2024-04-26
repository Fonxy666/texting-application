namespace Server.Model.Responses.Message;

public record SaveMessageResponse(bool Success, Server.Model.Chat.Message? Message, string? errorMessage);