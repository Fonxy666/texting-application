﻿namespace Server.Model.Responses.Message;

public record MessageResponse(bool Success, string? RoomId, string? ErrorMessage);