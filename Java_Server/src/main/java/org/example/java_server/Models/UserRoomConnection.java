package org.example.java_server.Models;

public class UserRoomConnection {
    private String user;
    private String room;

    public UserRoomConnection(String user, String room) {
        this.user = user;
        this.room = room;
    }

    public String getUser() {
        return user;
    }

    public void setUser(String user) {
        this.user = user;
    }

    public String getRoom() {
        return room;
    }

    public void setRoom() {
        this.room = room;
    }
}
