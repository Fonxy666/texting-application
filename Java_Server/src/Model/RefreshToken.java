package Model;

public class RefreshToken {
    private String Token;
    private java.time.LocalTime Created;
    private java.time.LocalTime Expires;

    public RefreshToken(String token) {
        this.Token = token;
        this.Created = java.time.LocalTime.now();
        this.Expires = java.time.LocalTime.now();
    }

    public String getToken() {
        return Token;
    }

    public java.time.LocalTime getCreated() {
        return this.Created;
    }

    public java.time.LocalTime getExpires() {
        return this.Expires;
    }
}
