namespace ChatService.Model.Responses.Message;

public record ChatMessageResponse(bool Success, string? RoomId, string? ErrorMessage);