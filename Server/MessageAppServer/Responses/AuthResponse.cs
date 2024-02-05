namespace Server.Responses;

public record AuthResponse(string Email, string Username, string Token);