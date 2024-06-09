import { HttpClient } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { ErrorHandlerService } from '../../../services/error-handler.service';
import { MessageService } from 'primeng/api';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { CookieService } from 'ngx-cookie-service';
import { FriendRequest } from '../../../model/FriendRequest';
import { FriendService } from '../../../services/friend-service/friend.service';
import { FriendRequestManage } from '../../../model/FriendRequestManage';
import { FriendRequestManageRequest } from '../../../model/FriendRequestManageRequest';
import { FriendRequestManageWithReceiverId } from '../../../model/FriendRequestManageWithReceiverId';

@Component({
  selector: 'app-send-friend-request',
  templateUrl: './send-friend-request.component.html',
  styleUrls: ['./send-friend-request.component.css'],
  providers: [MessageService]
})
export class SendFriendRequestComponent implements OnInit {
    public friendRequests: FriendRequestManage[] = [];
    public friends: FriendRequestManage[] = [];
    avatarUrl: { [key: string]: string } = {};
    userId: string = "";

    constructor(
        private friendService: FriendService,
        private fb: FormBuilder,
        private http: HttpClient,
        private errorHandler: ErrorHandlerService,
        private messageService: MessageService,
        private cookieService: CookieService
    ) { }

    friendName!: FormGroup;

    ngOnInit(): void {
        this.userId = this.cookieService.get("UserId");
        this.friendName = this.fb.group({
            userName: ['', [Validators.required]]
        });
    
        this.friendService.friendRequests$.subscribe(res => {
            this.friendRequests = res;
            res.forEach(request => {
                this.loadUserAvatar(request.senderId);
                this.loadUserAvatar(request.receiverId);
            })
        });

        this.friendService.friends$.subscribe(res => {
            this.friends = res;
            res.forEach(request => {
                this.loadUserAvatar(request.senderId);
                this.loadUserAvatar(request.receiverId);
            })
        })
    
        this.getPendingFriendRequests();
        this.getFriendRequests();
    }

    OnFormSubmit() {
        const friendRequest = new FriendRequest(this.userId, this.friendName.get('userName')?.value);

        this.http.post(`/api/v1/User/SendFriendRequest`, friendRequest, { withCredentials: true })
        .pipe(
            this.errorHandler.handleError401()
        )
        .subscribe(
            (response: FriendRequestManage) => {
                if (response) {
                    this.messageService.add({ severity: 'success', summary: 'Success', detail: `Friend request successfully sent to '${friendRequest.receiver}'.`, styleClass: 'ui-toast-message-success' });
                    this.friendService.sendFriendRequest(new FriendRequestManageWithReceiverId(response.requestId, response.senderName, response.senderId, response.sentTime, friendRequest.receiver));
                    this.friendName.reset();
                }
            },
            (error: any) => {
                console.log(error);
                if (error.status === 403) {
                    this.errorHandler.handleError403(error);
                } else if (error.status === 400) {
                    this.messageService.add({ severity: 'error', summary: 'Error', detail: `${error.error.message}` });
                } else {
                    console.log(error);
                }
            }
        );
    }

    getPendingFriendRequests() {
        this.http.get(`/api/v1/User/GetFriendRequests?userId=${this.userId}`, { withCredentials: true })
        .pipe(
            this.errorHandler.handleError401()
        )
        .subscribe(
            (response: FriendRequestManage[]) => {
                this.friendRequests = response;

                this.friendRequests.forEach(request => {
                    this.loadUserAvatar(request.senderId);
                    this.loadUserAvatar(request.receiverId);
                });
            },
            (error: any) => {
                console.log(error);
                if (error.status === 403) {
                    this.errorHandler.handleError403(error);
                } else {
                    console.log(error);
                }
            }
        );
    }

    getFriendRequests() {
        this.http.get(`/api/v1/User/GetFriends?userId=${this.userId}`, { withCredentials: true })
        .pipe(
            this.errorHandler.handleError401()
        )
        .subscribe(
            (response: FriendRequestManage[]) => {
                console.log(response);
                this.friends = response;

                this.friends.forEach(request => {
                    this.loadUserAvatar(request.senderId);
                    this.loadUserAvatar(request.receiverId);
                });
            },
            (error: any) => {
                console.log(error);
                if (error.status === 403) {
                    this.errorHandler.handleError403(error);
                } else {
                    console.log(error);
                }
            }
        );
    }

    displayRemainingTime(time: string) {
        const givenTime = new Date(time);
        const currentTime = new Date();

        let years = currentTime.getFullYear() - givenTime.getFullYear();
        let months = currentTime.getMonth() - givenTime.getMonth();
        let days = currentTime.getDate() - givenTime.getDate();
        let hours = currentTime.getHours() - givenTime.getHours();
        let minutes = currentTime.getMinutes() - givenTime.getMinutes();

        if (minutes < 0) {
            minutes += 60;
            hours--;
        }
        if (hours < 0) {
            hours += 24;
            days--;
        }
        if (days < 0) {
            const daysInPreviousMonth = new Date(currentTime.getFullYear(), currentTime.getMonth(), 0).getDate();
            days += daysInPreviousMonth;
            months--;
        }
        if (months < 0) {
            months += 12;
            years--;
        }

        const parts = [];
        if (years > 0) parts.push(`${years} year${years !== 1 ? 's' : ''}`);
        if (months > 0) parts.push(`${months} month${months !== 1 ? 's' : ''}`);
        if (days > 0) parts.push(`${days} day${days !== 1 ? 's' : ''}`);
        if (hours > 0) parts.push(`${hours} hour${hours !== 1 ? 's' : ''}`);
        if (minutes > 0) parts.push(`${minutes} minute${minutes !== 1 ? 's' : ''}`);

        return parts.join(', ');
    }

    displayUserName(name: string) {
        if (name.length <= 8) {
            return name;
        } else {
            return name.slice(0, 8) + '...';
        }
    }

    loadUserAvatar(userId: string) {
        this.http.get(`/api/v1/User/GetImage?userId=${userId}`, { withCredentials: true, responseType: 'blob' })
        .pipe(
            this.errorHandler.handleError401()
        )
        .subscribe(
            (response: Blob) => {
                const reader = new FileReader();
                reader.onloadend = () => {
                    this.avatarUrl[userId] = reader.result as string;
                };
                reader.readAsDataURL(response);
            },
            (error) => {
                if (error.status === 403) {
                    this.errorHandler.handleError403(error);
                }
                console.error(error);
                console.log("There is no Avatar for this user.");
                this.avatarUrl[userId] = "https://ptetutorials.com/images/user-profile.png";
            }
        );
    }

    handleFriendRequestAccept(request: FriendRequestManage) {
        const friendRequest = new FriendRequestManageRequest(request.requestId, this.userId);
        this.http.patch(`/api/v1/User/AcceptReceivedFriendRequest`, friendRequest, { withCredentials: true })
        .pipe(
            this.errorHandler.handleError401()
        )
        .subscribe(
            () => {
                this.friendService.acceptFriendRequest(new FriendRequestManage(request.requestId, request.senderName, request.senderId, request.sentTime, request.receiverName, request.receiverId));
            },
            (error) => {
                if (error.status === 403) {
                    this.errorHandler.handleError403(error);
                }
                console.error(error);
            }
        );
    }

    handleFriendRequestDecline(requestId: string) {
        const request = new FriendRequestManageRequest(requestId, this.userId);
        this.http.patch(`/api/v1/User/DeclineReceivedFriendRequest`, request, { withCredentials: true })
        .pipe(
            this.errorHandler.handleError401()
        )
        .subscribe(
            (response: any) => {
                console.log(response);
            },
            (error) => {
                if (error.status === 403) {
                    this.errorHandler.handleError403(error);
                }
                console.error(error);
            }
        );
    }
}