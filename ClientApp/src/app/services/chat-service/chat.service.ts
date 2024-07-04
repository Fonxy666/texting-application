import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject, Observable, Subject, catchError, throwError } from 'rxjs';
import { MessageRequest } from '../../model/message-requests/MessageRequest';
import { CookieService } from 'ngx-cookie-service';
import { ChangeMessageRequest } from '../../model/user-credential-requests/ChangeMessageRequest';
import { ChangeMessageSeenRequest } from '../../model/message-requests/ChangeMessageSeenRequest';
import { ConnectedUser } from '../../model/room-requests/ConnectedUser';
import { Router } from '@angular/router';
import { UserService } from '../user-service/user.service';
import { FriendService } from '../friend-service/friend.service';
import { CreateRoomRequest } from '../../model/room-requests/CreateRoomRequest';
import { HttpClient } from '@angular/common/http';
import { ErrorHandlerService } from '../error-handler-service/error-handler.service';
import { JoinRoomRequest } from '../../model/room-requests/JoinRoomRequest';
import { ChangePasswordRequestForRoom } from '../../model/room-requests/ChangePasswordRequestForRoom';

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

    constructor(
        private cookieService: CookieService,
        private router: Router,
        private userService: UserService,
        private friendService: FriendService,
        private http: HttpClient,
        private errorHandler: ErrorHandlerService
    ) {
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
            this.removeSessionStates()
            this.messages[this.currentRoom!] = [];
        } catch (error) {
            console.error('Error deleting message:', error);
        }
    }

    public async leaveChat() {
        try {
            this.removeSessionStates()
            this.messages[this.currentRoom!] = [];
            await this.connection.stop();
            console.log('Chat-SignalR connection stopped.');
        } catch (error) {
            console.error('Error stopping Chat-SignalR connection:', error);
        }
    }

    public setRoomCredentialsAndNavigate(roomName: any, roomId: string, senderId?: string) {
        if (this.userInRoom()) {
            this.leaveChat();
        };

        if (this.cookieService.get("Anonymous") === "True") {
            this.joinRoom("Anonymous", roomId)
            .then(_ => {
                this.router.navigate([`/message-room/${roomId}`]);
                sessionStorage.setItem("roomId", roomId);
                sessionStorage.setItem("room", roomName);
                sessionStorage.setItem("user", "Anonymous");
                if (senderId) {
                    this.friendService.handleChatInviteClick(roomId, senderId);
                }
            }).catch((err) => {
                console.log(err);
            })
        } else {
            this.joinRoom(this.userService.userName, roomId)
            .then(_ => {
                this.router.navigate([`/message-room/${roomId}`]);
                sessionStorage.setItem("roomId", roomId);
                sessionStorage.setItem("room", roomName);
                sessionStorage.setItem("user", this.userService.userName);
                if (senderId) {
                    this.friendService.handleChatInviteClick(roomId, senderId);
                }
            }).catch((err) => {
                console.log(err);
            })
        }
    };

    private removeSessionStates() {
        sessionStorage.removeItem("room");
        sessionStorage.removeItem("user");
        sessionStorage.removeItem("roomId");
    }

    private userInRoom():boolean {
        if (sessionStorage.getItem("room") && sessionStorage.getItem("user") && sessionStorage.getItem("roomId")) {
            return true;
        }

        return false;
    }

    registerRoom(form: CreateRoomRequest): Observable<any> {
        return this.errorHandler.handleErrors(
            this.http.post(`/api/v1/Chat/RegisterRoom`, form, { withCredentials: true })
        )
    }

    joinToRoom(form: JoinRoomRequest): Observable<any> {
        return this.errorHandler.handleErrors(
            this.http.post(`/api/v1/Chat/JoinRoom`, form, { withCredentials: true })
        )
    }

    saveMessage(form: MessageRequest): Observable<any> {
        return this.errorHandler.handleErrors(
            this.http.post(`api/v1/Message/SendMessage`, form, { withCredentials: true})
        )
    }

    getMessages(roomId: string): Observable<any> {
        return this.errorHandler.handleErrors(
            this.http.get(`/api/v1/Message/GetMessages/${roomId}`, { withCredentials: true })
        )
    }

    editMessage(request: ChangeMessageRequest): Observable<any> {
        return this.errorHandler.handleErrors(
            this.http.patch(`/api/v1/Message/EditMessage`, request, { withCredentials: true })
        )
    }

    editMessageSeen(request: ChangeMessageSeenRequest): Observable<any> {
        return this.errorHandler.handleErrors(
            this.http.patch(`/api/v1/Message/EditMessageSeen`, request, { withCredentials: true })
        )
    }

    messageDelete(messageId: string): Observable<any> {
        return this.errorHandler.handleErrors(
            this.http.delete(`/api/v1/Message/DeleteMessage?id=${messageId}`, { withCredentials: true})
        )
    }

    userIsTheCreator(roomId: string): Observable<any> {
        return this.errorHandler.handleErrors(
            this.http.get(`/api/v1/Chat/ExamineIfTheUserIsTheCreator?roomId=${roomId}`, { withCredentials: true})
        )
    }

    deleteRoomHttpRequest(roomId: string): Observable<any> {
        return this.errorHandler.handleErrors(
            this.http.delete(`/api/v1/Chat/DeleteRoom?roomId=${roomId}`, { withCredentials: true})
        )
    }

    changePasswordForRoom(form: ChangePasswordRequestForRoom): Observable<any> {
        return this.errorHandler.handleErrors(
            this.http.patch(`/api/v1/Chat/ChangePasswordForRoom`, form, { withCredentials: true})
        )
    }
}