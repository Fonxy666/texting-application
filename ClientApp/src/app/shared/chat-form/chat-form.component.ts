import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FriendService } from '../../core/services/friend-service/friend.service';
import { ShowFriendRequestData } from '../model/responses/user-responses.model';
import { FormGroup } from '@angular/forms';
import { ChangePasswordForRoomRequest } from '../model/room-requests/chat-requests.model';
import { ChatService } from '../../core/services/chat-service/chat.service';
import { ConnectedUser } from '../model/chat-models.model';
import { DisplayService } from '../../core/services/display-service/display.service';
import { CookieService } from 'ngx-cookie-service';
import { ChangeMessageRequest } from '../model/user-credential-requests/user-credentials-requestsmodel.';
import { ChangeMessageTextRequest, DeleteMessageRequest } from '../model/message-requests/MessageRequest';

@Component({
    selector: 'app-chat-form',
    templateUrl: './chat-form.component.html',
    styleUrls: ['./chat-form.component.css', '../../../styles.css']
})
export class ChatFormComponent {

    constructor(
        public chatService: ChatService,
        public friendService: FriendService,
        public displayService: DisplayService,
        private cookieService: CookieService
    ) {}

    searchTermForFriends: string = '';
    onlineFriends: ShowFriendRequestData[] | undefined;
    changePasswordRequest!: FormGroup;
    searchTerm: string = '';
    userId = this.cookieService.get("UserId")?? "";
    roomId = sessionStorage.getItem("roomId")?? "";
    roomName = sessionStorage.getItem("room")?? "";
    inputMessage: string = "";

    @Input() isLoading: boolean = false;
    @Input() isHumanChat: boolean = true;
    @Input() userIsTheCreator: boolean = false;
    @Input() avatars!: { [userId: string]: string };
    @Input() connectedUsers!: ConnectedUser[];
    @Input() messages: any[] = [];
    @Input() imageCount!: number;
    @Input() messageModifyBool: boolean = false;
    @Input() messageModifyRequest: ChangeMessageRequest = {id: "", text: "", iv: ""};
    @Input() showPassword: boolean = false;

    @Output() deleteRoom = new EventEmitter<void>();
    @Output() handleInviteToRoom = new EventEmitter<string>();
    @Output() changePasswordForRoom = new EventEmitter<ChangePasswordForRoomRequest>();
    @Output() leaveChat = new EventEmitter<boolean>();
    @Output() sendMessage = new EventEmitter<string>();
    @Output() sendMessageHttpRequest = new EventEmitter<ChangeMessageRequest>();
    @Output() closeMessageModify = new EventEmitter<void>();
    @Output() loadAvatarFromMessages = new EventEmitter<string[]>();
    @Output() messageModify = new EventEmitter<ChangeMessageTextRequest>();
    @Output() deleteMessage = new EventEmitter<DeleteMessageRequest>();

    callRoomDelete() {
        this.deleteRoom.emit();
    }

    callHandleInviteToRoom(name: string) {
        this.handleInviteToRoom.emit(name);
    }

    callChangePasswordForRoom() {
        const changePasswordRequest: ChangePasswordForRoomRequest = {
            id: this.roomId,
            oldPassword: this.changePasswordRequest.get('oldPassword')?.value,
            password: this.changePasswordRequest.get('newPassword')?.value
        };
                
        this.changePasswordForRoom.emit(changePasswordRequest);
    }

    callLeaveChat(bool: boolean) {
        this.leaveChat.emit(bool);
    }

    callSendMessage() {
        this.sendMessage.emit(this.inputMessage);
    }

    callSendMessageHttpRequest() {
        this.sendMessageHttpRequest.emit(this.messageModifyRequest);
    }

    callHandleCloseMessageModify() {
        this.closeMessageModify.emit();
    }

    callLoadAvatarFromMessages(seenList: string[]) {
        this.loadAvatarFromMessages.emit(seenList);
    }

    handleMessageModify(messageId: string, newText: string) {
        const changeMessageTextRequest: ChangeMessageTextRequest = {
            messageId: messageId,
            newText: newText
        };
        this.messageModify.emit(changeMessageTextRequest);
    }

    callMessageDelete(messageId: string) {
        var deleteMessageRequest: DeleteMessageRequest = {
            messageId: messageId
        }
        this.deleteMessage.emit(deleteMessageRequest);
    }

    searchInFriends() {
        if (this.searchTermForFriends.trim() === '') {
            this.friendService.onlineFriends$.subscribe(users => {
                this.onlineFriends = users;
            });
        } else {
            this.onlineFriends = this.friendService.onlineFriends$.value.filter(user =>
                this.userId !== user.senderId? user.senderName.toLowerCase().includes(this.searchTermForFriends.toLowerCase()) : user.receiverName.toLowerCase().includes(this.searchTermForFriends.toLowerCase())
            );
        }
    };

    searchInConnectedUsers() {
        if (this.searchTerm.trim() === '') {
            this.chatService.connectedUsers$.subscribe(users => {
                this.connectedUsers = users;
            });
        } else {
            this.connectedUsers = this.chatService.connectedUsers$.value.filter(user =>
                user.userName.toLowerCase().includes(this.searchTerm.toLowerCase())
            );
        }
    };

    examineIfNextMessageNotContainsUserId(userId: string, index: number) {
        if (this.chatService.messages[this.roomId] === undefined) {
            return;
        }

        const slicedMessages = this.chatService.messages[this.roomId].slice(index + 1);

        for (const message of slicedMessages) {
            if (message.messageData.seenList == null) {
                continue;
            }

            if (message.messageData.seenList.includes(userId)) {
                return false;
            }
        }

        this.imageCount++;
        return true;
    };

    resetImageCount() {
        this.imageCount = 0;
    };

    toggleShowPassword() {
        this.showPassword = !this.showPassword;
    }
}
