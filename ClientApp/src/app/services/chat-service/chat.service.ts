import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject, Subject } from 'rxjs';
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
    public messages$ = new BehaviorSubject<any[]>([]);
    public connectedUsers$ = new BehaviorSubject<ConnectedUser[]>([]);
    public messages: { [roomId: string]: any[] } = {};
    public users: ConnectedUser[] = [];
    public roomDeleted$: Subject<string> = new Subject<string>();
    private currentRoom: string | null = null;
    public messagesInitialized$ = new Subject<string>();

    constructor(private cookieService: CookieService) {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl('/chat')
            .configureLogging(signalR.LogLevel.Critical)
            .build();

        this.initializeConnection();

        this.connection.on("ReceiveMessage", (user: string, message: string, messageTime: string, userId: string, messageId: string, seenList: string[], roomId: string) => {
            if (!this.messages[roomId]) {
                this.messages[roomId] = [];
                this.messagesInitialized$.next(roomId);
            }
            if (userId !== this.cookieService.get("UserId")) {
                this.messages[roomId].push({ user, message, messageTime, userId, messageId, seenList });
            }
            if (this.currentRoom === roomId) {
                this.messages$.next([...this.messages[roomId]]);
            }
        });

        this.connection.on("ConnectedUser", (userDictionary: { [key: string]: string }) => {
            const users = Object.keys(userDictionary).map(userName => ({
                userId: userDictionary[userName],
                userName: userName
            }));
            this.connectedUsers$.next(users);
        });

        this.connection.on("UserDisconnected", (username: string) => {
            const updatedUsers = this.connectedUsers$.value.filter(user => user.userName !== username);
            this.connectedUsers$.next(updatedUsers);
        });

        

        this.connection.on("RoomDeleted", (roomId: string) => {
            this.roomDeleted$.next(roomId);
            delete this.messages[roomId];
            if (this.currentRoom === roomId) {
                this.messages$.next([]);
            }
        });
    }

    public setCurrentRoom(roomId: string) {
        this.currentRoom = roomId;
        this.messages$.next(this.messages[roomId] || []);
    }

    private async initializeConnection() {
        try {
            await this.start();
    
            const roomId = sessionStorage.getItem("roomId");
            const userName = sessionStorage.getItem("user");
            if (roomId && userName) {
                await this.joinRoom(userName, roomId);
            }
        } catch (error) {
            console.error('Chat-SignalR connection failed to start:', error);
        }
    
        this.connection.onclose(async () => {
            console.log('Chat-SignalR connection closed, attempting to reconnect...');
            await this.reconnect();
        });
    
        this.connection.onreconnecting(() => {
            console.log('Chat-SignalR connection is attempting to reconnect...');
        });
    }

    private async start() {
        try {
            await this.connection.start();
            console.log('Chat-SignalR connection established.');
        } catch (error) {
            console.error('Error starting Chat-SignalR connection:', error);
            throw error;
        }
    }

    private async reconnect() {
        try {
            await this.start();
            console.log('Chat-SignalR reconnected successfully.');
        } catch (error) {
            console.error('Chat-SignalR reconnection failed:', error);
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

    public async deleteRoom(roomId: string) {
        try {
            await this.connection.invoke("OnRoomDelete", roomId);
            sessionStorage.removeItem("room");
            sessionStorage.removeItem("user");
            sessionStorage.removeItem("roomId");
            this.messages[this.currentRoom!] = [];
        } catch (error) {
            console.error('Error deleting message:', error);
        }
    }

    public async leaveChat() {
        try {
            sessionStorage.removeItem("room");
            sessionStorage.removeItem("user");
            sessionStorage.removeItem("roomId");
            this.messages[this.currentRoom!] = [];
            await this.connection.stop();
            console.log('Chat-SignalR connection stopped.');
        } catch (error) {
            console.error('Error stopping Chat-SignalR connection:', error);
        }
    }
}