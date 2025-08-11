import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject, Observable } from 'rxjs';
import { CookieService } from 'ngx-cookie-service';
import { isEqual } from 'lodash';
import { HttpClient } from '@angular/common/http';
import { ErrorHandlerService } from '../error-handler-service/error-handler.service';
import { CryptoService } from '../crypto-service/crypto.service';
import { ShowFriendRequestData } from '../../model/responses/user-responses.model';
import { ChatRoomInviteRequest, ChatRoomInviteResponse, DeleteFriendRequest, FriendRequestManage } from '../../model/friend-requests/friend-requests.model';
import { StoreRoomSymmetricKeyRequest } from '../../model/room-requests/chat-requests.model';
import { ServerResponse } from '../../model/responses/shared-response.model';

@Injectable({
    providedIn: 'root'
})
export class FriendService {
    public connection: signalR.HubConnection;
    public friendRequests$ = new BehaviorSubject<ShowFriendRequestData[]>([]);
    public friendRequests: { [userId: string]: ShowFriendRequestData[] } = {};
    public friends$ = new BehaviorSubject<ShowFriendRequestData[]>([]);
    public friends: { [userId: string]: ShowFriendRequestData[] } = {};
    public chatRoomInvites$ = new BehaviorSubject<ChatRoomInviteRequest[]>([]);
    public chatRoomInvites: { [userId: string]: ChatRoomInviteRequest[] } = {};
    public onlineFriends$ = new BehaviorSubject<ShowFriendRequestData[]>([]);
    public onlineFriends: { [userId: string]: ShowFriendRequestData[] } = {};
    public loggedIn: boolean = this.cookieService.check("UserId");
    public announceNumber$ = new BehaviorSubject<number>(0);
    public announceNumber: { [userId: string]: number } = {};

    constructor(
        private cookieService: CookieService,
        private http: HttpClient,
        private errorHandler: ErrorHandlerService,
        private cryptoService: CryptoService
    ) {
        this.connection = new signalR.HubConnectionBuilder()
        .withUrl('/friend', { accessTokenFactory: () => this.cookieService.get('UserId') })
        .configureLogging(signalR.LogLevel.Critical)
        .build();
        
        let userId = this.cookieService.get("UserId");
        if (userId !== "") {
            this.initializeConnection();
        }
        
        if (this.loggedIn) {
            this.loadInitialData();
        }

        this.connection.on("ReceiveFriendRequest", (request: ShowFriendRequestData) => {
            const newRequest: ShowFriendRequestData = {
                requestId: request.requestId,
                senderName: request.senderName,
                senderId: request.senderId,
                sentTime: new Date(request.sentTime),
                receiverName: request.receiverName,
                receiverId: request.receiverId!
            };
        
            this.addRequest(newRequest);
        });

        this.connection.on("AcceptFriendRequest", (request: FriendRequestManage) => {
            const newRequest: ShowFriendRequestData = {
                requestId: request.requestId,
                senderName: request.senderName,
                senderId: request.senderId,
                sentTime: new Date(request.sentTime),
                receiverName: request.receiverName,
                receiverId: request.receiverId!
            };

            this.updateFriendRequests(newRequest);
        });

        this.connection.on("DeleteFriendRequest", (requestId: string) => {
            this.updateFriendRequestsWithDeclinedRequest(requestId);
        });

        this.connection.on("DeleteFriend", (requestId: string) => {
            this.deleteFromFriends(requestId);
        });

        this.connection.on("ReceiveChatRoomInvite", async (request: ChatRoomInviteResponse) => {
            if (request.roomKey !== null) {
                const keyRequest: StoreRoomSymmetricKeyRequest = {
                    encryptedKey: request.roomKey!,
                    roomId: request.roomId
                };

                this.cryptoService.sendEncryptedRoomKey(keyRequest)
                    .subscribe(() => {
                        this.updateChatInvites(request);
                    }) 
            } else {
                this.updateChatInvites(request);
            }
        });

        this.connection.on("ReceiveOnlineFriends", (onlineFriends: ShowFriendRequestData[]) => {
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

    public async sendFriendRequest(request: ShowFriendRequestData) {
        try {
            await this.connection.invoke("SendFriendRequest", request);
        } catch (error) {
            console.error('Error sending friend request via SignalR:', error);
        }
    }

    private addRequest(request: ShowFriendRequestData) {
        const userId = this.cookieService.get('UserId');

        if (typeof request.sentTime === 'string') {
            request.sentTime = new Date(request.sentTime);
        }

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

    public async acceptFriendRequest(request: ShowFriendRequestData) {
        try {
            await this.connection.invoke("AcceptFriendRequest", request);
            this.updateFriendRequests(request);
        } catch (error) {
            console.error('Error accepting friend request via SignalR:', error);
        }
    }
    
    private updateFriendRequests(request: ShowFriendRequestData) {
        const userId = this.cookieService.get('UserId');

        if (typeof request.sentTime === 'string') {
            request.sentTime = new Date(request.sentTime);
        }
        
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

    public async deleteFriendRequest(request: DeleteFriendRequest) {
        try {
            await this.connection.invoke("DeleteFriendRequest", request);
            this.updateFriendRequestsWithDeclinedRequest(request.requestId);
        } catch (error) {
            console.error('Error declining friend request via SignalR:', error);
        }
    }

    private updateFriendRequestsWithDeclinedRequest(requestId: string) {
        const userId = this.cookieService.get('UserId');
        this.friendRequests[userId] = this.friendRequests[userId].filter(r => r.requestId !== requestId);
        this.friendRequests$.next(this.friendRequests[userId]);
    }

    public async deleteFriend(request: DeleteFriendRequest) {
        try {
            await this.connection.invoke("DeleteFriend", request);
            this.deleteFromFriends(request.requestId);
        } catch (error) {
            console.error('Error deleting friend via SignalR:', error);
        }
    }

    private deleteFromFriends(requestId: string) {
        const userId = this.cookieService.get('UserId');
        this.friends[userId] = this.friends[userId].filter(r => r.requestId !== requestId)
        this.friends$.next(this.friends[userId]);
    }

    public async sendChatRoomInvite(request: ChatRoomInviteRequest) {
        try {
            await this.connection.invoke("SendChatRoomInvite", request);
        } catch (error) {
            console.error('Error declining friend request via SignalR:', error);
        }
    }

    private updateChatInvites(request: ChatRoomInviteResponse) {
        if (!this.chatRoomInvites[request.receiverId]) {
            this.chatRoomInvites[request.receiverId] = [];
        }
        
        this.chatRoomInvites[request.receiverId].push(request);
        
        this.chatRoomInvites$.next(this.chatRoomInvites[request.receiverId]);
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
            (response: ServerResponse<ShowFriendRequestData[]>) => {
                if (!this.friends[userId]) {
                    this.friends[userId] = [];
                }

                if (!this.friendRequests[userId]) {
                    this.friendRequests[userId] = [];
                }
    
                if (response.isSuccess) {
                    response.data.forEach(res => {
                        const requestList = this.friendRequests[userId];
                        
                        if (!requestList.some(request => isEqual(request, res))) {
                            if (typeof res.sentTime === 'string') {
                                res.sentTime = new Date(res.sentTime);
                            }

                            requestList.push(res);
                        }
    
                        this.friendRequests$.next(requestList);
                    });
                }
            }
        );
    }

    private saveFriends() {
        const userId = this.cookieService.get("UserId");

        this.getFriends()
        .subscribe(
            (response: ServerResponse<ShowFriendRequestData[]>) => {
                if (!this.friends[userId]) {
                    this.friends[userId] = [];
                }
    
                if (response.isSuccess) {
                    response.data.forEach(res => {
                        const friendsList = this.friends[userId];
                        
                        if (!friendsList.some(friend => isEqual(friend, res))) {
                            if (typeof res.sentTime === 'string') {
                                res.sentTime = new Date(res.sentTime);
                            }

                            friendsList.push(res);
                        }
        
                        this.friends$.next(friendsList);
                    });
                }
            }
        );
    }

    sendFriendRequestHttp(friendName: string): Observable<ServerResponse<ShowFriendRequestData>> {
        return this.errorHandler.handleErrors(
            this.http.post<ServerResponse<ShowFriendRequestData>>(`/api/v1/User/SendFriendRequest`, JSON.stringify(friendName), {
                headers: {
                    'Content-Type': 'application/json'
                },
                withCredentials: true
            })
        )
    }

    getPendingFriendRequests(): Observable<ServerResponse<ShowFriendRequestData[]>> {
        return this.errorHandler.handleErrors(
            this.http.get<ServerResponse<ShowFriendRequestData[]>>(`/api/v1/User/GetFriendRequests`, { withCredentials: true })
        )
    }

    private getFriends(): Observable<ServerResponse<ShowFriendRequestData[]>> {
        return this.errorHandler.handleErrors(
            this.http.get<ServerResponse<ShowFriendRequestData[]>>(`/api/v1/User/GetFriends`, { withCredentials: true })
        )
    }

    acceptFriendRequestHttp(requestId: string): Observable<ServerResponse<void>> {
        return this.errorHandler.handleErrors(
            this.http.patch<ServerResponse<void>>(`/api/v1/User/AcceptReceivedFriendRequest`, JSON.stringify(requestId), { 
                headers: {
                    'Content-Type': 'application/json'
                },
                withCredentials: true
            })
        )
    }

    friendRequestDecline(requestId: string, userType: string): Observable<ServerResponse<void>> {
        return this.errorHandler.handleErrors(
            this.http.delete<ServerResponse<void>>(`/api/v1/User/DeleteFriendRequest?requestId=${requestId}&userType=${userType}`, { 
                headers: {
                    'Content-Type': 'application/json'
                },
                withCredentials: true
            })
        )
    }

    deleteFriendHttp(requestId: string): Observable<ServerResponse<void>> {
        return this.errorHandler.handleErrors(
            this.http.delete<ServerResponse<void>>(`/api/v1/User/DeleteFriend?requestId=${requestId}`, { withCredentials: true })
        )
    }
}