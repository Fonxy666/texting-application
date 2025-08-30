import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FriendService } from '../../core/services/friend-service/friend.service';
import { ShowFriendRequestData } from '../model/responses/user-responses.model';
import { FormGroup } from '@angular/forms';
import { ChangePasswordForRoomRequest } from '../model/room-requests/chat-requests.model';
import { ChatService } from '../../core/services/chat-service/chat.service';
import { ConnectedUser } from '../model/chat-models.model';
import { DisplayService } from '../../core/services/display-service/display.service';
import { CookieService } from 'ngx-cookie-service';
import { ChangeMessageRequest, CheckMessageSeenListRequest } from '../model/user-credential-requests/user-credentials-requestsmodel.';

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
    @Output() examineNextMessageSeenList = new EventEmitter<CheckMessageSeenListRequest>();
    @Output() sendMessage = new EventEmitter<string>();
    @Output() sendMessageHttpRequest = new EventEmitter<ChangeMessageRequest>();
    @Output() closeMessageModify = new EventEmitter<void>();

    callRoomDelete() {
        this.deleteRoom.emit();
    }

    callHandleInviteToRoom(name: string) {
        this.handleInviteToRoom.emit(name);
    }

    callExamineNextMessageSeenList(userId: string, index: number) {
        let request: CheckMessageSeenListRequest = {
            UserId: userId,
            Index: index
        };
        this.examineNextMessageSeenList.emit(request);
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
}
