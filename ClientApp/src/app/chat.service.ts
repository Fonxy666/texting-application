import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})

export class ChatService {

    public connection : any = new signalR.HubConnectionBuilder()
        .withUrl('http://localhost:5000/chat')
        .configureLogging(signalR.LogLevel.Information)
        .build();

    public message$ = new BehaviorSubject<any>([]);
    public connectedUsers = new BehaviorSubject<string[]>([]);
    public messages: any[] = [];
    public users: string[] = [];

    constructor() {
        this.start();

        this.connection.on("ReceiveMessage", (user: String, message: String, messageTime: String) => {
            console.log("User:", user);
            console.log("Message:", message);
            console.log("Message time:", messageTime);
            this.messages = [...this.messages, {user, message, messageTime}];
            this.message$.next(this.messages);
        })

        this.connection.on("ConnectedUser", (users: any) => {
            this.connectedUsers.next(users);
            console.log(users);
        })
    }

    public async start() {
        try{
            await this.connection.start();
            console.log("Connection is established!")
        } catch (error) {
            console.log(error);
            setTimeout(() => {
                this.start();
            }, 5000);
        }
    }

    public async joinRoom(user: string, room: string) {
        this.connection.invoke("JoinRoom", {user, room});
    }

    public async sendMessage(message: string) {
        return this.connection.invoke("SendMessage", message);
    }

    public async leaveChat() {
        return this.connection.stop();
    }
}
