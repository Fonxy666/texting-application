namespace UserService.Models.Responses;

public record ChatRoomInviteResponse(string RoomId, string RoomName, string ReceiverName, string ReceiverId,  string SenderId, string SenderName, string? RoomKey);