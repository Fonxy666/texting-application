import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject } from 'rxjs';
import { CookieService } from 'ngx-cookie-service';
import { FriendRequestManage } from '../../model/FriendRequestManage';
import { FriendRequestManageWithReceiverId } from '../../model/FriendRequestManageWithReceiverId';

@Injectable({
    providedIn: 'root'
})
export class FriendService {
    public connection: signalR.HubConnection;
    public friendRequests$ = new BehaviorSubject<FriendRequestManage[]>([]);
    public friends$ = new BehaviorSubject<FriendRequestManage[]>([]);

    constructor(private cookieService: CookieService) {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl('/friend', { accessTokenFactory: () => this.cookieService.get('UserId') })
            .configureLogging(signalR.LogLevel.Critical)
            .build();

        this.initializeConnection();

        this.connection.on("ReceiveFriendRequest", (requestId: string, senderName: string, senderId: string, sentTime: string) => {
            this.addRequest(new FriendRequestManage(requestId, senderName, senderId, sentTime));
        });

        this.connection.on("AcceptFriendRequest", (requestId: string, senderName: string, senderId: string, sentTime: string) => {
            this.updateFriendRequests(new FriendRequestManage(requestId, senderName, senderId, sentTime));
        });
    }

    private async initializeConnection() {
        try {
            await this.start();
            await this.joinHub(this.cookieService.get("UserId"));

            this.connection.onclose(async () => {
                console.log('Friend-SignalR connection closed, attempting to reconnect...');
                await this.reconnect();
            });

            this.connection.onreconnecting(() => {
                console.log('Friend-SignalR connection is attempting to reconnect...');
            });

        } catch (error) {
            console.error('Friend-SignalR connection failed to start:', error);
        }
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

    public async sendFriendRequest(request: FriendRequestManageWithReceiverId) {
        try {
            await this.connection.invoke("SendFriendRequest", request.requestId, request.senderName, request.senderId, request.sentTime, request.receiverName);
        } catch (error) {
            console.error('Error sending friend request via SignalR:', error);
        }
    }

    private addRequest(request: FriendRequestManage) {
        const currentRequests = this.friendRequests$.value;
        const updatedRequests = [...currentRequests, request];
        this.friendRequests$.next(updatedRequests);
    }

    public async acceptFriendRequest(request: FriendRequestManage) {
        try {
            const sentTime = request.sentTime.toString();
            await this.connection.invoke("AcceptFriendRequest", request.requestId, request.senderName, request.senderId, sentTime);
            this.updateFriendRequests(request);
        } catch (error) {
            console.error('Error accepting friend request via SignalR:', error);
        }
    }
    
    private updateFriendRequests(request: FriendRequestManage) {
        const currentRequests = this.friendRequests$.value.filter(r => r.requestId !== request.requestId);
        const updatedFriends = [...this.friends$.value, request];
    
        this.friendRequests$.next(currentRequests);
        this.friends$.next(updatedFriends);
    }
}