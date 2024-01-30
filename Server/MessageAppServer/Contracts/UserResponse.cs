namespace Server.Model;

public record UserResponse(string? Email, bool TwoFactorEnabled) {}