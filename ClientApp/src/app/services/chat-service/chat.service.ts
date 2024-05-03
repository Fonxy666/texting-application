import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject } from 'rxjs';
import { MessageRequest } from '../../model/MessageRequest';
import { CookieService } from 'ngx-cookie-service';
import { ChangeMessageRequest } from '../../model/ChangeMessageRequest';
import { ChangeMessageSeenRequest } from '../../model/ChangeMessageSeenRequest';
import { ConnectedUser } from '../../model/ConnectedUser';

@Injectable({
    providedIn: 'root'
})

export class ChatService {
    public connection: signalR.HubConnection;
    public message$ = new BehaviorSubject<any>([]);
    public connectedUsers = new BehaviorSubject<ConnectedUser[]>([]);
    public messages: any[] = [];
    public users: ConnectedUser[] = [];

    constructor(private cookieService: CookieService) {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl('/chat')
            .configureLogging(signalR.LogLevel.Information)
            .build();

        this.initializeConnection();

        this.connection.on("ReceiveMessage", (user: string, message: string, messageTime: string, userId: string, messageId: string, seenList: string[]) => {
            if (userId !== this.cookieService.get("UserId")) {
                this.messages.push({ user, message, messageTime, userId, messageId, seenList });
            }
            this.message$.next(this.messages);
        });

        this.connection.on("ConnectedUser", (userDictionary: { [key: string]: string }) => {
            const users = Object.keys(userDictionary).map(userName => ({
                userId: userDictionary[userName],
                userName: userName
            }));
            this.connectedUsers.next(users);
        });

        this.connection.on("UserDisconnected", (username: string) => {
            const updatedUsers = this.connectedUsers.value.filter(user => user.userName !== username);
            this.connectedUsers.next(updatedUsers);
        });
    }

    private async initializeConnection() {
        try {
            await this.start();
    
            const roomName = sessionStorage.getItem("room")?? "asd";
            const userName = sessionStorage.getItem("user")?? "Fonxy666";
            if (roomName && userName) {
                await this.joinRoom(userName, roomName);
            }
        } catch (error) {
            console.error('SignalR connection failed to start:', error);
        }
    
        this.connection.onclose(async () => {
            console.log('SignalR connection closed, attempting to reconnect...');
            await this.reconnect();
        });
    
        this.connection.onreconnecting(() => {
            console.log('SignalR connection is attempting to reconnect...');
        });
    }

    private async start() {
        try {
            await this.connection.start();
            console.log('SignalR connection established.');
        } catch (error) {
            console.error('Error starting SignalR connection:', error);
            throw error;
        }
    }

    private async reconnect() {
        try {
            await this.start();
            console.log('SignalR reconnected successfully.');
        } catch (error) {
            console.error('SignalR reconnection failed:', error);
            setTimeout(() => this.reconnect(), 5000);
        }
    }

    public async joinRoom(user: string, room: string) {
        try {
            await this.connection.invoke("JoinRoom", { user, room });
        } catch (error) {
            console.error('Error joining room:', error);
        }
    }

    public async sendMessage(message: MessageRequest) {
        try {
            await this.connection.invoke("SendMessage", message);
        } catch (error) {
            console.error('Error sending message:', error);
        }
    }

    public async modifyMessage(request: ChangeMessageRequest) {
        try {
            await this.connection.invoke("ModifyMessage", request);
        } catch (error) {
            console.error('Error modifying the message:', error);
        }
    }

    public async modifyMessageSeen(request: ChangeMessageSeenRequest) {
        try {
            await this.connection.invoke("ModifyMessageSeen", request);
        } catch (error) {
            console.error('Error modifying the message:', error);
        }
    }

    public async deleteMessage(messageId: string) {
        try {
            await this.connection.invoke("DeleteMessage", messageId);
        } catch (error) {
            console.error('Error deleting message:', error);
        }
    }

    public async leaveChat() {
        try {
            sessionStorage.removeItem("room");
            sessionStorage.removeItem("user");
            await this.connection.stop();
            console.log('SignalR connection stopped.');
        } catch (error) {
            console.error('Error stopping SignalR connection:', error);
        }
    }
}