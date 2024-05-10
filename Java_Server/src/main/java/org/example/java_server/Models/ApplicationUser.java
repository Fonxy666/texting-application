package org.example.java_server.Models;

import jakarta.persistence.Entity;
import jakarta.persistence.GeneratedValue;
import jakarta.persistence.GenerationType;
import jakarta.persistence.*;

import java.security.MessageDigest;
import java.security.NoSuchAlgorithmException;
import java.util.UUID;

@Entity
@Table(name = "Users")
public class ApplicationUser {
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private UUID Id;
    private String ImageUrl;
    private String RefreshToken;
    private java.time.LocalTime RefreshTokenCreated;
    private java.time.LocalTime RefreshTokenExpires;
    private String UserName;
    private String NormalizedUserName;
    private String Email;
    private String NormalizedEmail;
    private Boolean EmailConfirmed;
    private String PasswordHash;
    private String PhoneNumber;
    private UUID SecurityStamp;
    private Boolean PhoneNumberConfirmed;
    private UUID ConcurrencyStamp;
    private Boolean TwoFactorEnabled;
    private java.time.LocalTime LockoutEnd;
    private Boolean LockoutEnabled;
    private Integer AccessFailedCount;

    public ApplicationUser() { }

    public ApplicationUser(String imageUrl, String password, String refreshToken, String userName, String email, Boolean emailConfirmed, String phoneNumber) {
        this.Id = UUID.randomUUID();
        this.ImageUrl = imageUrl;
        this.RefreshToken = refreshToken;
        this.RefreshTokenCreated = java.time.LocalTime.now();
        this.RefreshTokenExpires = java.time.LocalTime.now();
        this.UserName = userName;
        this.NormalizedUserName = userName.toUpperCase();
        this.Email = email;
        this.NormalizedEmail = email.toUpperCase();
        this.EmailConfirmed = emailConfirmed;
        this.PasswordHash = hashPassword(password);
        this.PhoneNumber = phoneNumber;
        this.SecurityStamp = UUID.randomUUID();
        this.PhoneNumberConfirmed = false;
        this.ConcurrencyStamp = UUID.randomUUID();
        this.TwoFactorEnabled = true;
        this.LockoutEnd = null;
        this.LockoutEnabled = false;
        this.AccessFailedCount = 0;
    }

    public String hashPassword(String password) {
        try {
            MessageDigest digest = MessageDigest.getInstance("SHA-256");
            byte[] hashedBytes = digest.digest(password.getBytes());
            return bytesToHex(hashedBytes);
        } catch (NoSuchAlgorithmException e) {
            e.printStackTrace();
            return null; // Handle error appropriately
        }
    }

    public boolean passwordMatch(String enteredPassword, String storedHashedPassword) {
        String enteredPasswordHash = hashPassword(enteredPassword);
        return storedHashedPassword.equals(enteredPasswordHash);
    }

    private String bytesToHex(byte[] bytes) {
        StringBuilder sb = new StringBuilder();
        for (byte b : bytes) {
            sb.append(String.format("%02x", b));
        }
        return sb.toString();
    }

    public String getPassword() {
        return this.PasswordHash;
    }
}
