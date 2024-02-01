namespace Server.Contracts;

public record UserResponse(string? Email, bool TwoFactorEnabled);