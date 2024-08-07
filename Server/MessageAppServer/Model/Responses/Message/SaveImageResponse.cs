namespace Server.Model.Responses.Message;

public record SaveImageResponse(bool Success, Server.Model.Chat.Image? Message, string? errorMessage);