﻿namespace Server.Model.Responses.User;

public record UserResponse(string? UserName, string? Email, bool TwoFactorEnabled);