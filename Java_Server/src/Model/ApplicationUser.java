package Model;

import java.util.UUID;

public class ApplicationUser {
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
    private String PasswordHas;
    private String PhoneNumber;
    private String SecurityStamp;
    private Boolean PhoneNumberConfirmed;
    private String ConcurrencyStamp;
    private Boolean TwoFactorEnabled;
    private java.time.LocalTime LockoutEnd;
    private Boolean LockoutEnabled;
    private Integer AccessFailedCount;

    public ApplicationUser(UUID id, String imageUrl, String refreshToken, java.time.LocalTime refreshTokenCreated, java.time.LocalTime refreshTokenExpires, String userName, String normalizedUserName, String email, String normalizedEmail, Boolean emailConfirmed, String passwordHas, String phoneNumber, String securityStamp, Boolean phoneNumberConfirmed, String concurrencyStamp, Boolean twoFactorEnabled, java.time.LocalTime lockoutEnd, Boolean lockoutEnabled, Integer accessFailedCount) {

    }
}
