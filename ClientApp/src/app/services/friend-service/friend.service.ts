import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject, Observable } from 'rxjs';
import { CookieService } from 'ngx-cookie-service';
import { FriendRequestManage } from '../../model/friend-requests/FriendRequestManage';
import { FriendRequestManageWithReceiverId } from '../../model/friend-requests/FriendRequestManageWithReceiverId';
import { isEqual } from 'lodash';
import { HttpClient } from '@angular/common/http';
import { ErrorHandlerService } from '../error-handler-service/error-handler.service';
import { ChatRoomInvite } from '../../model/room-requests/ChatRoomInvite';

@Injectable({
    providedIn: 'root'
})
export class FriendService {
    public connection: signalR.HubConnection;
    public friendRequests$ = new BehaviorSubject<FriendRequestManage[]>([]);
    public friendRequests: { [userId: string]: FriendRequestManage[] } = {};
    public friends$ = new BehaviorSubject<FriendRequestManage[]>([]);
    public friends: { [userId: string]: FriendRequestManage[] } = {};
    public chatRoomInvites$ = new BehaviorSubject<ChatRoomInvite[]>([]);
    public chatRoomInvites: { [userId: string]: ChatRoomInvite[] } = {};
    public onlineFriends$ = new BehaviorSubject<FriendRequestManage[]>([]);
    public onlineFriends: { [userId: string]: FriendRequestManage[] } = {};
    public loggedIn: boolean = this.cookieService.check("UserId");
    public announceNumber$ = new BehaviorSubject<number>(0);
    public announceNumber: { [userId: string]: number } = {};

    constructor(
        private cookieService: CookieService,
        private http: HttpClient,
        private errorHandler: ErrorHandlerService
    ) {
        this.connection = new signalR.HubConnectionBuilder()
        .withUrl('/friend', { accessTokenFactory: () => this.cookieService.get('UserId') })
        .configureLogging(signalR.LogLevel.Critical)
        .build();
        
        this.initializeConnection();
        
        if (this.loggedIn) {
            this.loadInitialData();
        }

        this.connection.on("ReceiveFriendRequest", (requestId: string, senderName: string, senderId: string, sentTime: string, receiverName: string, receiverId: string) => {
            this.addRequest(new FriendRequestManage(requestId, senderName, senderId, sentTime, receiverName, receiverId));
        });

        this.connection.on("AcceptFriendRequest", (requestId: string, senderName: string, senderId: string, sentTime: string, receiverName: string, receiverId: string) => {
            this.updateFriendRequests(new FriendRequestManage(requestId, senderName, senderId, sentTime, receiverName, receiverId));
        });

        this.connection.on("DeleteFriendRequest", (requestId: string) => {
            this.updateFriendRequestsWithDeclinedRequest(requestId);
        });

        this.connection.on("DeleteFriend", (requestId: string) => {
            this.deleteFromFriends(requestId);
        });

        this.connection.on("ReceiveChatRoomInvite", (roomId: string, roomName: string, receiverId: string, senderId: string, senderName: string) => {
            this.updateChatInvites(roomId, roomName, receiverId, senderId, senderName);
        });

        this.connection.on("ReceiveOnlineFriends", (onlineFriends: FriendRequestManage[]) => {
            this.onlineFriends$.next(onlineFriends);
        });
    }

    public async loadInitialData() {
        try {
            await Promise.all([this.savePendingFriendRequests(), this.saveFriends()]);
        } catch (error) {
            console.error('Error loading initial data:', error);
        }
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
    
        if (!this.friendRequests[request.receiverId].some(friend => isEqual(friend.requestId, request.requestId))) {
            this.friendRequests[request.receiverId].push(request);
        }

        if (!this.friendRequests[request.senderId].some(friend => isEqual(friend.requestId, request.requestId))) {
            this.friendRequests[request.senderId].push(request);
        }

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
        
        if (!this.friendRequests[userId]) {
            this.friendRequests[userId] = [];
        }
        if (!this.friends[userId]) {
            this.friends[userId] = [];
        }
    
        this.friendRequests[userId] = this.friendRequests[userId].filter(r => r.requestId !== request.requestId);
        
        this.friends[userId].push(request);
    
        this.friendRequests$.next(this.friendRequests[userId]);
        this.friends$.next(this.friends[userId]);
    }

    public async deleteFriendRequest(requestId: string, senderId: string, receiverId: string) {
        try {
            await this.connection.invoke("DeleteFriendRequest", requestId, senderId, receiverId);
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

    public async sendChatRoomInvite(roomId: string, roomName: string, receiverName: string, senderId: string, senderName: string) {
        try {
            await this.connection.invoke("SendChatRoomInvite", roomId, roomName, receiverName, senderId, senderName);
        } catch (error) {
            console.error('Error declining friend request via SignalR:', error);
        }
    }

    private updateChatInvites(roomId: string, roomName: string, receiverId: string, senderId: string, senderName: string) {
        if (!this.chatRoomInvites[receiverId]) {
            this.chatRoomInvites[receiverId] = [];
        }
        
        this.chatRoomInvites[receiverId].push(new ChatRoomInvite(senderId, roomId, roomName, senderName));
        
        this.chatRoomInvites$.next(this.chatRoomInvites[receiverId]);
    }

    public handleChatInviteClick(roomId: string, senderId: string) {
        const userId = this.cookieService.get('UserId');
        this.chatRoomInvites[userId] = this.chatRoomInvites[userId].filter(request => {
            return request.roomId !== roomId && request.senderId !== senderId
        })

        this.chatRoomInvites$.next(this.chatRoomInvites[userId]);
    }

    public async getOnlineFriends() {
        try {
            await this.connection.invoke("GetOnlineFriends", this.cookieService.get('UserId'));
        } catch (error) {
            console.error('Error fetching online friends:', error);
        }
    }

    private savePendingFriendRequests() {
        const userId = this.cookieService.get("UserId");

        this.getPendingFriendRequests()
        .subscribe(
            (response: FriendRequestManage[]) => {
                if (!this.friendRequests[userId]) {
                    this.friendRequests[userId] = [];
                }
    
                response.forEach(res => {
                    const requestList = this.friendRequests[userId];
                    
                    if (!requestList.some(request => isEqual(request, res))) {
                        requestList.push(res);
                    }

                    this.friendRequests$.next(requestList);
                });
            }
        );
    }

    getPendingFriendRequests(): Observable<any> {
        return this.errorHandler.handleErrors(
            this.http.get(`/api/v1/User/GetFriendRequests`, { withCredentials: true })
        )
    }

    private getFriends(): Observable<any> {
        return this.errorHandler.handleErrors(
            this.http.get(`/api/v1/User/GetFriends`, { withCredentials: true })
        )
    }

    private saveFriends() {
        const userId = this.cookieService.get("UserId");

        this.getFriends()
        .subscribe(
            (response: FriendRequestManage[]) => {
                if (!this.friends[userId]) {
                    this.friends[userId] = [];
                }
    
                response.forEach(res => {
                    const friendsList = this.friends[userId];
                    
                    if (!friendsList.some(friend => isEqual(friend, res))) {
                        friendsList.push(res);
                    }
    
                    this.friends$.next(friendsList);
                });
            }
        );
    }

    sendFriendRequestHttp(friendName: string): Observable<any> {
        return this.errorHandler.handleErrors(
            this.http.post(`/api/v1/User/SendFriendRequest`, JSON.stringify(friendName), {
                headers: {
                    'Content-Type': 'application/json'
                },
                withCredentials: true
            })
        )
    }

    acceptFriendRequestHttp(requestId: string) {
        return this.errorHandler.handleErrors(
            this.http.patch(`/api/v1/User/AcceptReceivedFriendRequest`, JSON.stringify(requestId), {
                headers: {
                    'Content-Type': 'application/json'
                },
                withCredentials: true
            })
        )
    }

    friendRequestDecline(requestId: string, userType: string) {
        return this.errorHandler.handleErrors(
            this.http.delete(`/api/v1/User/DeleteFriendRequest?requestId=${requestId}&userType=${userType}`, {
                headers: {
                    'Content-Type': 'application/json'
                },
                withCredentials: true
            })
        )
    }

    deleteFriendHttp(requestId: string) {
        return this.errorHandler.handleErrors(
            this.http.delete(`/api/v1/User/DeleteFriend?connectionId=${requestId}`, { withCredentials: true })
        )
    }
}