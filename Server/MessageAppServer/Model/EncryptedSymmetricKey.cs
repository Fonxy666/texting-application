﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Server.Model.Chat;

namespace Server.Model;

public class EncryptedSymmetricKey
{
    [Key]
    public Guid KeyId { get; init; }
    public Guid UserId { get; init; }
    
    [ForeignKey("UserId")]
    public ApplicationUser User { get; init; }
    public Guid RoomId { get; init; }
    
    [ForeignKey("RoomId")]
    public Room Room { get; init; }
    public string EncryptedKey { get; init; }
    
    public EncryptedSymmetricKey() { }
    
    public EncryptedSymmetricKey(Guid userId, string encryptedKey, Guid roomId)
    {
        KeyId = Guid.NewGuid();
        UserId = userId;
        RoomId = roomId;
        EncryptedKey = encryptedKey;
    }
}