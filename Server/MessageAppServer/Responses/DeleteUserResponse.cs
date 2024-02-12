namespace Server.Responses;

public record DeleteUserResponse(string Username, string Message, bool Successful);