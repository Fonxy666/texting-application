import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject } from 'rxjs';
import { MessageRequest } from './model/MessageRequest';

@Injectable({
    providedIn: 'root'
})

export class ChatService {
    public connection : any = new signalR.HubConnectionBuilder()
        .withUrl('https://localhost:7045/chat')
        .configureLogging(signalR.LogLevel.Information)
        .build();

    public message$ = new BehaviorSubject<any>([]);
    public connectedUsers = new BehaviorSubject<string[]>([]);
    public messages: any[] = [];
    public users: string[] = [];

    constructor() {
        this.start();

        this.connection.on("ReceiveMessage", (user: String, message: String, messageTime: String) => {
            this.messages = [...this.messages, {user, message, messageTime}];
            this.message$.next(this.messages);
        });

        this.connection.on("ConnectedUser", (users: any) => {
            this.connectedUsers.next(users);
        });

        this.connection.on("UserDisconnected", (username: string) => {
            const updatedUsers = this.connectedUsers.value.filter(user => user !== username);
            this.connectedUsers.next(updatedUsers);
        });
    }

    public async start() {
        try{
            await this.connection.start();
        } catch (error) {
            console.log(error);
            setTimeout(() => {
                this.start();
            }, 0);
        }
    }

    public async joinRoom(user: string, room: string) {
        this.connection.invoke("JoinRoom", {user, room});
    }

    public async sendMessage(message: MessageRequest) {
        return this.connection.invoke("SendMessage", message);
    }

    public async leaveChat() {
        return this.connection.stop();
    }
}
