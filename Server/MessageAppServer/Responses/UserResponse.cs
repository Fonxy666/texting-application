namespace Server.Responses;

public record UserResponse(string? UserName, string? Email, bool TwoFactorEnabled);