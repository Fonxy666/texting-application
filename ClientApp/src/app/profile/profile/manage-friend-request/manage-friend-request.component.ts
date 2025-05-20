import { Component, OnInit } from '@angular/core';
import { MessageService } from 'primeng/api';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { CookieService } from 'ngx-cookie-service';
import { FriendService } from '../../../services/friend-service/friend.service';
import { MediaService } from '../../../services/media-service/media.service';
import { DisplayService } from '../../../services/display-service/display.service';
import { ShowFriendRequestData, UserResponse, UserResponseFailure } from '../../../model/responses/user-responses.model';
import { DeleteFriendRequest } from '../../../model/friend-requests/friend-requests.model';
import { UserService } from '../../../services/user-service/user.service';

@Component({
  selector: 'app-manage-friend-request',
  templateUrl: './manage-friend-request.component.html',
  styleUrls: ['./manage-friend-request.component.css'],
  providers: [MessageService]
})
export class ManageFriendRequestComponent implements OnInit {
    avatarUrl: { [key: string]: string } = {};
    userId: string = "";
    friendRequests: ShowFriendRequestData[] | undefined;
    friends: ShowFriendRequestData[] | undefined;

    constructor(
        private friendService: FriendService,
        private fb: FormBuilder,
        private messageService: MessageService,
        private cookieService: CookieService,
        private mediaService: MediaService,
        public displayService: DisplayService,
        private userService: UserService
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
        console.log( typeof(this.userService.userName))
        if (this.userService.userName === friendName) {
            this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: `You can not send friend request to yourself.`
                });
            return;
        }
        this.friendService.sendFriendRequestHttp(friendName)
        .subscribe(
            (response: UserResponse<ShowFriendRequestData>) => {
                if (response.isSuccess) {
                    this.messageService.add({
                        severity: 'success',
                        summary: 'Success',
                        detail: `Friend request successfully sent to '${friendName}'.`,
                        styleClass: 'ui-toast-message-success'
                    });
                    const newRequest: ShowFriendRequestData = {
                        requestId: response.data.requestId,
                        senderName: response.data.senderName,
                        senderId: response.data.senderId,
                        sentTime: new Date(response.data.sentTime),
                        receiverName: friendName,
                        receiverId: response.data.receiverId
                    }
                    this.friendService.sendFriendRequest(newRequest);
                    this.friendName.reset();
                }
            },
            (error: UserResponseFailure) => {
                console.log(error);
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: `${error.error?.message}`
                });
            }
        );
    }

    handleFriendRequestAccept(request: ShowFriendRequestData) {
        this.friendService.acceptFriendRequestHttp(request.requestId)
        .subscribe(
            (response: UserResponse<void>) => {
                if (response.isSuccess) {
                    let newRequest: ShowFriendRequestData = {
                        requestId: request.requestId,
                        senderName: request.senderName,
                        senderId: request.senderId,
                        sentTime: new Date(request.sentTime),
                        receiverName: request.receiverName,
                        receiverId: request.receiverId
                    }

                    this.friendService.acceptFriendRequest(newRequest);
                }
            }
        );
    }

    handleFriendRequestDecline(requestId: string, senderId: string, receiverId: string, userType: string) {
        const declineRequest: DeleteFriendRequest = {
            requestId: requestId,
            senderId: senderId,
            receiverId: receiverId
        }
        this.friendService.friendRequestDecline(requestId, userType)
        .subscribe(
            (response: UserResponse<void>) => {
                if (response.isSuccess) {
                    this.friendService.deleteFriendRequest(declineRequest);
                }
            }
        );
    }

    deleteFriend(requestId: string, receiverId: string, senderId: string) {
        const declineRequest: DeleteFriendRequest = {
            requestId: requestId,
            senderId: senderId,
            receiverId: receiverId
        }
        this.friendService.deleteFriendHttp(requestId)
        .subscribe(
            (response: UserResponse<void>) => {
                if (response.isSuccess) {
                    this.friendService.deleteFriend(declineRequest);
                }
            }
        );
    }
}