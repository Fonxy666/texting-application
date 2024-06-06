import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject } from 'rxjs';
import { FriendRequest } from '../../model/FriendRequest';
import { CookieService } from 'ngx-cookie-service';

@Injectable({
    providedIn: 'root'
})
export class FriendService {
    public connection: signalR.HubConnection;
    public friendRequests$ = new BehaviorSubject<FriendRequest[]>([]);

    constructor(private cookieService: CookieService) {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl('/friend', { accessTokenFactory: () => this.cookieService.get('UserId') })
            .configureLogging(signalR.LogLevel.Critical)
            .build();

        this.initializeConnection();

        this.connection.on("ReceiveFriendRequest", (senderId: string, receiver: string) => {
            this.addRequest({ senderId, receiver });
        });
    }

    private async initializeConnection() {
        try {
            await this.start();
            await this.joinHub(this.cookieService.get("UserId"));
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
            console.log('Friend-SignalR connection established.');
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

    public async joinHub(userId: string) {
        try {
            await this.connection.invoke("JoinToHub", userId);
        } catch (error) {
            console.error('Error joining hub:', error);
        }
    }

    public async sendFriendRequest(request: FriendRequest) {
        try {
            await this.connection.invoke("SendFriendRequest", request.senderId, request.receiver);
        } catch (error) {
            console.error('Error sending friend request:', error);
        }
    }

    private addRequest(request: FriendRequest) {
        const currentRequests = this.friendRequests$.value;
        const updatedRequests = [...currentRequests, request];
        this.friendRequests$.next(updatedRequests);
    }
}