package org.example.java_server.Models.Chat;

import java.security.MessageDigest;
import java.security.NoSuchAlgorithmException;
import java.util.UUID;

public class Room {
    private UUID RoomId;
    private String RoomName;
    private String Password;

    public Room(String roomName, String password) {
        this.RoomId = UUID.randomUUID();
        this.RoomName = roomName;
        this.Password = hashPassword(password);
    }

    public UUID getRoomId() {
        return this.RoomId;
    }

    public String getRoomName() {
        return this.RoomName;
    }

    public static String hashPassword(String password) {
        try {
            MessageDigest digest = MessageDigest.getInstance("SHA-256");
            byte[] hashedBytes = digest.digest(password.getBytes());
            return bytesToHex(hashedBytes);
        } catch (NoSuchAlgorithmException e) {
            e.printStackTrace();
            return null; // Handle error appropriately
        }
    }

    public static boolean passwordMatch(String enteredPassword, String storedHashedPassword) {
        String enteredPasswordHash = hashPassword(enteredPassword);
        return storedHashedPassword.equals(enteredPasswordHash);
    }

    private static String bytesToHex(byte[] bytes) {
        StringBuilder sb = new StringBuilder();
        for (byte b : bytes) {
            sb.append(String.format("%02x", b));
        }
        return sb.toString();
    }
}
