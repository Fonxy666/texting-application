namespace Server.Responses;

public record UserResponse(string? Email, bool TwoFactorEnabled);