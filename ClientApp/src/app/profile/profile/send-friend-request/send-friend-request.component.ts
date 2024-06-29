import { HttpClient } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { MessageService } from 'primeng/api';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { CookieService } from 'ngx-cookie-service';
import { FriendRequest } from '../../../model/FriendRequest';
import { FriendService } from '../../../services/friend-service/friend.service';
import { FriendRequestManage } from '../../../model/FriendRequestManage';
import { FriendRequestManageRequest } from '../../../model/FriendRequestManageRequest';
import { FriendRequestManageWithReceiverId } from '../../../model/FriendRequestManageWithReceiverId';
import { MediaService } from '../../../services/media-service/media.service';
import { DisplayService } from '../../../services/display-service/display.service';
import { ErrorHandlerService } from '../../../services/error-handler-service/error-handler.service';

@Component({
  selector: 'app-send-friend-request',
  templateUrl: './send-friend-request.component.html',
  styleUrls: ['./send-friend-request.component.css'],
  providers: [MessageService]
})
export class SendFriendRequestComponent implements OnInit {
    avatarUrl: { [key: string]: string } = {};
    userId: string = "";
    friendRequests: FriendRequestManage[] | undefined;
    friends: FriendRequestManage[] | undefined;

    constructor(
        private friendService: FriendService,
        private fb: FormBuilder,
        private http: HttpClient,
        private errorHandler: ErrorHandlerService,
        private messageService: MessageService,
        private cookieService: CookieService,
        private mediaService: MediaService,
        public displayService: DisplayService
    ) { }

    friendName!: FormGroup;

    ngOnInit(): void {
        this.userId = this.cookieService.get("UserId");
        this.friendName = this.fb.group({
            userName: ['', [Validators.required]]
        });
    
        this.friendService.friendRequests$.subscribe(requests => {
            this.friendRequests = requests;
            requests.forEach(request => {
                this.mediaService.getAvatarImage(request.senderId).subscribe((image) => {
                    this.avatarUrl[request.senderId] = image;
                });
                this.mediaService.getAvatarImage(request.receiverId).subscribe((image) => {
                    this.avatarUrl[request.receiverId] = image;
                });
            });
        });
    
        this.friendService.friends$.subscribe(friends => {
            this.friends = friends;
            friends.forEach(request => {
                this.mediaService.getAvatarImage(request.senderId).subscribe((image) => {
                    this.avatarUrl[request.senderId] = image;
                });
                this.mediaService.getAvatarImage(request.receiverId).subscribe((image) => {
                    this.avatarUrl[request.receiverId] = image;
                });
            });
        });

        console.log(this.friendRequests);
    }

    OnFormSubmit() {
        const friendName = this.friendName.get('userName')?.value;
        this.http.post(`/api/v1/User/SendFriendRequest`, JSON.stringify(friendName), {
            headers: {
                'Content-Type': 'application/json'
            },
            withCredentials: true
        })
        .pipe(
            this.errorHandler.handleError401()
        )
        .subscribe(
            (response: FriendRequestManage) => {
                if (response) {
                    this.messageService.add({ severity: 'success', summary: 'Success', detail: `Friend request successfully sent to '${friendName}'.`, styleClass: 'ui-toast-message-success' });
                    this.friendService.sendFriendRequest(new FriendRequestManageWithReceiverId(response.requestId, response.senderName, response.senderId, response.sentTime, friendName));
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

    handleFriendRequestAccept(request: FriendRequestManage) {
        this.http.patch(`/api/v1/User/AcceptReceivedFriendRequest`, JSON.stringify(request.requestId), {
            headers: {
                'Content-Type': 'application/json'
            },
            withCredentials: true
        })
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

    handleFriendRequestDecline(requestId: string, senderId: string, receiverId: string, userType: string) {
        this.http.delete(`/api/v1/User/DeleteFriendRequest?requestId=${requestId}&userType=${userType}`, {
            headers: {
                'Content-Type': 'application/json'
            },
            withCredentials: true
        })
        .pipe(
            this.errorHandler.handleError401()
        )
        .subscribe(
            () => {
                this.friendService.deleteFriendRequest(requestId, senderId, receiverId);
            },
            (error) => {
                if (error.status === 403) {
                    this.errorHandler.handleError403(error);
                }
                console.error(error);
            }
        );
    }

    deleteFriend(requestId: string, receiverId: string, senderId: string) {
        console.log(requestId);
        this.http.delete(`/api/v1/User/DeleteFriend?connectionId=${requestId}`, { withCredentials: true })
        .pipe(
            this.errorHandler.handleError401()
        )
        .subscribe(
            (response) => {
                if (response) {
                    this.friendService.deleteFriend(requestId, receiverId, senderId);
                }
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