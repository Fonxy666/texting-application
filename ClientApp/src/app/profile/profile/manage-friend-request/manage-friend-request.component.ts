import { Component, OnInit } from '@angular/core';
import { MessageService } from 'primeng/api';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { CookieService } from 'ngx-cookie-service';
import { FriendService } from '../../../services/friend-service/friend.service';
import { FriendRequestManage } from '../../../model/friend-requests/FriendRequestManage';
import { FriendRequestManageWithReceiverId } from '../../../model/friend-requests/FriendRequestManageWithReceiverId';
import { MediaService } from '../../../services/media-service/media.service';
import { DisplayService } from '../../../services/display-service/display.service';

@Component({
  selector: 'app-manage-friend-request',
  templateUrl: './manage-friend-request.component.html',
  styleUrls: ['./manage-friend-request.component.css'],
  providers: [MessageService]
})
export class ManageFriendRequestComponent implements OnInit {
    avatarUrl: { [key: string]: string } = {};
    userId: string = "";
    friendRequests: FriendRequestManage[] | undefined;
    friends: FriendRequestManage[] | undefined;

    constructor(
        private friendService: FriendService,
        private fb: FormBuilder,
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
    }

    OnFormSubmit() {
        const friendName = this.friendName.get('userName')?.value;
        this.friendService.sendFriendRequestHttp(friendName)
        .subscribe(
            (response: any) => {
                if (response) {
                    this.messageService.add({
                        severity: 'success',
                        summary: 'Success',
                        detail: `Friend request successfully sent to '${friendName}'.`,
                        styleClass: 'ui-toast-message-success'
                    });
                    this.friendService.sendFriendRequest(
                        new FriendRequestManageWithReceiverId(
                            response.requestId,
                            response.senderName,
                            response.senderId,
                            response.sentTime,
                            friendName));
                    this.friendName.reset();
                }
            },
            (error: any) => {
                if (error.status === 400) {
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Error',
                        detail: `${error.error.message}`
                    });
                }
            }
        );
    }

    handleFriendRequestAccept(request: FriendRequestManage) {
        this.friendService.acceptFriendRequestHttp(request.requestId)
        .subscribe(
            () => {
                this.friendService.acceptFriendRequest(
                    new FriendRequestManage(
                        request.requestId,
                        request.senderName,
                        request.senderId,
                        request.sentTime,
                        request.receiverName,
                        request.receiverId
                    )
                );
            }
        );
    }

    handleFriendRequestDecline(requestId: string, senderId: string, receiverId: string, userType: string) {
        this.friendService.friendRequestDecline(requestId, userType)
        .subscribe(
            () => {
                this.friendService.deleteFriendRequest(requestId, senderId, receiverId);
            }
        );
    }

    deleteFriend(requestId: string, receiverId: string, senderId: string) {
        this.friendService.deleteFriendHttp(requestId)
        .subscribe(
            (response) => {
                if (response) {
                    this.friendService.deleteFriend(requestId, receiverId, senderId);
                }
            }
        );
    }
}