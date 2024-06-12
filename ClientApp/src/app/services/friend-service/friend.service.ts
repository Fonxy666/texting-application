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
    public friendRequests: { [userId: string]: FriendRequestManage[] } = {};
    public friends$ = new BehaviorSubject<FriendRequestManage[]>([]);
    public friends: { [userId: string]: FriendRequestManage[] } = {};

    constructor(private cookieService: CookieService) {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl('/friend', { accessTokenFactory: () => this.cookieService.get('UserId') })
            .configureLogging(signalR.LogLevel.Critical)
            .build();

        this.initializeConnection();

        this.connection.on("ReceiveFriendRequest", (requestId: string, senderName: string, senderId: string, sentTime: string, receiverName: string, receiverId: string) => {
            this.addRequest(new FriendRequestManage(requestId, senderName, senderId, sentTime, receiverName, receiverId));
        });

        this.connection.on("AcceptFriendRequest", (requestId: string, senderName: string, senderId: string, sentTime: string, receiverName: string, receiverId: string) => {
            this.updateFriendRequests(new FriendRequestManage(requestId, senderName, senderId, sentTime, receiverName, receiverId));
        });

        this.connection.on("DeclineFriendRequest", (requestId: string) => {
            this.updateFriendRequestsWithDeclinedRequest(requestId);
        });

        this.connection.on("DeleteFriend", (requestId: string) => {
            this.deleteFromFriends(requestId);
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
        const userId = this.cookieService.get('UserId');

        if (!this.friendRequests[request.receiverId]) {
            this.friendRequests[request.receiverId] = [];
        }
        if (!this.friendRequests[request.senderId]) {
            this.friendRequests[request.senderId] = [];
        }
    
        this.friendRequests[request.receiverId].push(request);
        this.friendRequests[request.senderId].push(request);
        this.friendRequests$.next(this.friendRequests[userId]);
    }

    public async acceptFriendRequest(request: FriendRequestManage) {
        try {
            await this.connection.invoke("AcceptFriendRequest", request.requestId, request.senderName, request.senderId, request.sentTime, request.receiverName);
            this.updateFriendRequests(request);
        } catch (error) {
            console.error('Error accepting friend request via SignalR:', error);
        }
    }
    
    private updateFriendRequests(request: FriendRequestManage) {
        const userId = this.cookieService.get('UserId');
        this.friendRequests[userId] = this.friendRequests[userId].filter(r => r.requestId !== request.requestId);
        this.friends[userId] = [...this.friends[userId], request];
    
        this.friendRequests$.next(this.friendRequests[userId]);
        this.friends$.next(this.friends[userId]);
    }

    public async declineFriendRequest(requestId: string) {
        try {
            await this.connection.invoke("DeclineFriendRequest", requestId);
            this.updateFriendRequestsWithDeclinedRequest(requestId);
        } catch (error) {
            console.error('Error declining friend request via SignalR:', error);
        }
    }

    private updateFriendRequestsWithDeclinedRequest(requestId: string) {
        const userId = this.cookieService.get('UserId');
        this.friendRequests[userId] = this.friendRequests[userId].filter(r => r.requestId !== requestId);
        this.friendRequests$.next(this.friendRequests[userId]);
    }

    public async deleteFriend(requestId: string, receiverId: string, senderId: string) {
        try {
            await this.connection.invoke("DeleteFriend", requestId, receiverId, senderId);
            this.deleteFromFriends(requestId);
        } catch (error) {
            console.error('Error deleting friend via SignalR:', error);
        }
    }

    private deleteFromFriends(requestId: string) {
        const userId = this.cookieService.get('UserId');
        this.friends[userId] = this.friends[userId].filter(r => r.requestId !== requestId)
        this.friends$.next(this.friends[userId]);
    }
}