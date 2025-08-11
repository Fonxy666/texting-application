namespace ChatService.Model.Responses.Message;

public record ChatMessageResponse(bool Success, string? RoomId, string? ErrorMessage);
public record SaveMessageResponse(bool Success, Model.Message? Message, string? errorMessage);
public record MessageDto(Guid MessageId, Guid SenderId, Guid RoomId, string Text, string SendTime, bool SentAsAnonymous, IList<Guid> SeenList, string Iv);
