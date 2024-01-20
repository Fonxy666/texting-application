namespace Server.Services.Authentication;

public record AuthResponse(string Email, string Username, string Token);