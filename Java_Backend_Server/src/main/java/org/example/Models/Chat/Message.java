package org.example.Models.Chat;

import java.time.LocalTime;
import java.util.ArrayList;
import java.util.List;
import java.util.UUID;

public class Message {
    private final UUID MessageId;
    private final String RoomId;
    private final String SenderId;
    private String Text;
    private final java.time.LocalTime SendTime;
    private final String SentAsAnonymous;
    private List<UUID> Seen = new ArrayList<UUID>();

    public Message(String roomId, String senderId, String text, String sentAsAnonymous) {
        this.MessageId = UUID.randomUUID();
        this.RoomId = roomId;
        this.SenderId = senderId;
        this.Text = text;
        this.SendTime = java.time.LocalTime.now();
        this.SentAsAnonymous = sentAsAnonymous;

        try {
            UUID senderUuid = UUID.fromString(senderId);
            this.Seen.add(senderUuid);
        } catch (IllegalArgumentException e) {
            System.err.println("Invalid UUID format for senderId: " + senderId);
        }
    }

    public Message(String messageId, String roomId, String senderId, String text, String sentAsAnonymous) {
        UUID messageId1;
        try {
            messageId1 = UUID.fromString(messageId);
        } catch (IllegalArgumentException e) {
            messageId1 = UUID.randomUUID();
            System.err.println("Invalid UUID format for messageId: " + senderId);
        }

        this.MessageId = messageId1;
        this.RoomId = roomId;
        this.SenderId = senderId;
        this.Text = text;
        this.SendTime = java.time.LocalTime.now();
        this.SentAsAnonymous = sentAsAnonymous;

        try {
            UUID senderUuid = UUID.fromString(senderId);
            this.Seen.add(senderUuid);
        } catch (IllegalArgumentException e) {
            System.err.println("Invalid UUID format for senderId: " + senderId);
        }
    }

    public UUID getMessageId() {
        return this.MessageId;
    }

    public String getRoomId() {
        return this.RoomId;
    }

    public String getSenderId() {
        return this.SenderId;
    }

    public LocalTime getSendTime() {
        return this.SendTime;
    }

    public String getSentAsAnonymous() {
        return this.SentAsAnonymous;
    }

    public void setSeenList(UUID seenId) {
        this.Seen.add(seenId);
    }

    public List<UUID> getSeenList() {
        return this.Seen;
    }

    public String getText() {
        return Text;
    }

    public void setText(String text) {
        Text = text;
    }
}
