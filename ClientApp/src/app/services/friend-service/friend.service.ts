import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject } from 'rxjs';
import { ConnectedUser } from '../../model/ConnectedUser';
import { FriendRequest } from '../../model/FriendRequest';

@Injectable({
    providedIn: 'root'
})

export class FriendService {
    public connection: signalR.HubConnection;
    public connectedUsers$ = new BehaviorSubject<ConnectedUser[]>([]);
    public users: ConnectedUser[] = [];
    public friendRequests = new BehaviorSubject<any[]>([]);

    constructor() {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl('/friend')
            .configureLogging(signalR.LogLevel.Critical)
            .build();

        this.initializeConnection();

        this.connection.on("ReceiveFriendRequest", (senderId: string, receiverId: string) => {
            console.log(`Friend request from ${senderId} to ${receiverId}`);
        });
    }

    private async initializeConnection() {
        try {
            await this.start();
        } catch (error) {
            console.error('Friend-SignalR connection failed to start:', error);
        }
    
        this.connection.onclose(async () => {
            console.log('Friend-SignalR connection closed, attempting to reconnect...');
            await this.reconnect();
        });
    
        this.connection.onreconnecting(() => {
            console.log('Friend-SignalR connection is attempting to reconnect...');
        });
    }

    private async start() {
        try {
            await this.connection.start();
            console.log('Friends-SignalR connection established.');
        } catch (error) {
            console.error('Error starting Friend-SignalR connection:', error);
            throw error;
        }
    }

    private async reconnect() {
        try {
            await this.start();
            console.log('Friend-SignalR reconnected successfully.');
        } catch (error) {
            console.error('Friend-SignalR reconnection failed:', error);
            setTimeout(() => this.reconnect(), 5000);
        }
    }

    public async sendFriendRequest(request: FriendRequest) {
        try {
            await this.connection.invoke("OnFriendRequestSend", request);
        } catch (error) {
            console.error('Error sending friend request:', error);
        }
    }
}