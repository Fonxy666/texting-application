﻿namespace Server.Model.Responses.User;

public record ShowFriendRequestResponse(Guid RequestId, string SenderName, string SenderId, DateTime? SentTime, string ReceiverName, string ReceiverId);