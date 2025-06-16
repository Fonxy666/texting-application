import { AfterViewChecked, ChangeDetectionStrategy, Component, ElementRef, HostListener, OnInit, ViewChild } from '@angular/core';
import { ChatService } from '../../services/chat-service/chat.service';
import { Router } from '@angular/router';
import { combineLatest, firstValueFrom, forkJoin, from, of, Subscription } from 'rxjs';
import { filter, switchMap, take } from 'rxjs/operators';
import { MessageRequest } from '../../model/message-requests/MessageRequest';
import { CookieService } from 'ngx-cookie-service';
import { ChangeMessageSeenRequest } from '../../model/message-requests/ChangeMessageSeenRequest';
import { MessageService } from 'primeng/api';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { passwordMatchValidator, passwordValidator } from '../../validators/ValidPasswordValidator';
import { FriendService } from '../../services/friend-service/friend.service';
import { DisplayService } from '../../services/display-service/display.service';
import { MediaService } from '../../services/media-service/media.service';
import { UserService } from '../../services/user-service/user.service';
import { CryptoService } from '../../services/crypto-service/crypto.service';
import { IndexedDBService } from '../../services/db-service/indexed-dbservice.service';
import { ShowFriendRequestData } from '../../model/responses/user-responses.model';
import { ChangeMessageRequest } from '../../model/user-credential-requests/user-credentials-requestsmodel.';
import { ChatRoomInviteRequest } from '../../model/friend-requests/friend-requests.model';
import { ChangePasswordForRoomRequest, GetMessagesRequest } from '../../model/room-requests/chat-requests.model';
import { ConnectedUser } from '../../model/chat-models.model';
import { ReceiveMessageResponse } from '../../model/responses/chat-responses.model';
import { ServerResponse } from '../../model/responses/shared-response.model';

@Component({
  selector: 'app-chat',
  templateUrl: './chat.component.html',
  styleUrls: ['./chat.component.css', '../../../styles.css'],
  changeDetection: ChangeDetectionStrategy.Default,
  providers: [ MessageService ]
})

export class ChatComponent implements OnInit, AfterViewChecked {
    userId: string = "";
    roomId: string = "";
    inputMessage: string = "";
    roomName: string = sessionStorage.getItem("room")?? "";
    connectedUsers: ConnectedUser[] = [];
    searchTerm: string = '';
    searchTermForFriends: string = '';
    messageModifyBool: boolean = false;
    messageModifyRequest: ChangeMessageRequest = {id: "", message: "", iv: ""};
    isPageVisible = true;
    imageCount: number = 0;
    userIsTheCreator: boolean = false;
    showPassword: boolean = false;
    isLoading: boolean = false;
    private subscriptions: Subscription = new Subscription();
    onlineFriends: ShowFriendRequestData[] | undefined;
    userKey: string | null = null;

    @ViewChild('scrollMe') public scrollContainer!: ElementRef;
    @ViewChild('messageInput') public inputElement!: ElementRef;

    private routeSub!: Subscription;

    constructor(
        public chatService: ChatService,
        public router: Router,
        private cookieService: CookieService,
        private messageService: MessageService,
        private fb: FormBuilder,
        public friendService: FriendService,
        public displayService: DisplayService,
        private mediaService: MediaService,
        private userService: UserService,
        private cryptoService: CryptoService,
        private dbService: IndexedDBService
    ) { }

    messages: any[] = [];
    avatars: { [userId: string]: string } = {};
    changePasswordRequest!: FormGroup;
    
    ngOnInit(): void {
        this.userId = this.cookieService.get("UserId");
        this.roomId = sessionStorage.getItem("roomId")!;
        this.roomName = sessionStorage.getItem("room")!;

        this.chatService.setCurrentRoom(this.roomId);

        from(this.dbService.getEncryptionKey(this.userId)).pipe(
            filter((key): key is string => key !== null),
            switchMap(key => {
                this.userKey = key;
                return combineLatest([
                    of(key),
                    this.chatService.messages$
                ]);
            })
            ).subscribe(async ([key, data]) => {
                if (data.length < 1 || this.roomId === null) return;
                const decryptedRoomKey = await this.cryptoService.getDecryptedRoomKey(key, this.roomId);
                if (!decryptedRoomKey) {
                    console.error("Cannot get room key.");
                    return;
                }
            
                const decryptedMessages = await Promise.all(data.map(async innerData => {

                if (innerData.encrypted) {
                    try {
                        innerData.messageData.message = await this.cryptoService.decryptMessage(
                            innerData.messageData.message,
                            decryptedRoomKey,
                            innerData.messageData.iv
                        );

                        innerData.encrypted = false;
                        return innerData;
                    } catch (error) {
                        console.log("Failed to decrypt message:", innerData, error);
                    }
                } else {
                    return innerData;
                }
            }));
        
            this.messages = decryptedMessages.filter(Boolean);
        
            this.messages.forEach(() => {
                this.mediaService.getAvatarImage(this.userId).subscribe(image => {
                    this.avatars[this.userId] = image;
                });
            });
        });

        if (this.roomId) {
                this.subscriptions.add(
                this.chatService.messagesInitialized$
                    .pipe(
                        filter((initializedRoomId) => initializedRoomId === this.roomId),
                        take(1)
                    )
                    .subscribe(() => {
                        this.getMessages();
                    })
            );
        } else {
            console.error('No roomId found in session storage.');
        }

        this.chatService.connection.on("ModifyMessage", async (messageId: string, messageText: string) => {
            const decryptedRoomKey = await this.cryptoService.getDecryptedRoomKey(this.userKey!, this.roomId);

            if (decryptedRoomKey === null) {
                console.error("Cannot get room key.");
            }

            this.messages.forEach(async (data) => {
                if (data.messageData.messageId == messageId && data.encrypted) {
                    data.messageData.message = await this.cryptoService.decryptMessage(messageText, decryptedRoomKey!, data.messageData.iv);
                }
            })
        });

        this.chatService.connection.on("ModifyMessageSeen", (userIdFromSignalR: string) => {
            this.chatService.messages[this.roomId].forEach((message) => {
                if (!message.seenList) {
                    return;
                } else if (!message.seenList.includes(userIdFromSignalR)) {
                    message.seenList.push(userIdFromSignalR);
                }
            })
        });

        this.chatService.connection.on("DeleteMessage", (messageId: string) => {
            this.messages.forEach((data: any) => {
                if (data.messageData.messageId == messageId) {
                    data.messageData.message = "Deleted message.";
                }
            });
        });

        this.chatService.connectedUsers$.subscribe((users) => {
            this.connectedUsers = users;
            users.forEach((user) => {
                this.mediaService.getAvatarImage(user.userId).subscribe(
                    (avatar) => {
                        this.avatars[user.userId] = avatar;
                    },
                    (error) => {
                        console.log(error);
                    }
                );
            });
        });

        this.chatService.roomDeleted$.subscribe((deletedRoomId: string) => {
            if (deletedRoomId === this.roomId && !this.userIsTheCreator) {
                this.leaveChat(false);
                this.router.navigate(['/join-room'], { queryParams: { roomDeleted: 'true' } });
            }
        });

        this.getMessages();
        this.userIsTheCreatorMethod();

        this.changePasswordRequest = this.fb.group({
            oldPassword: ['', [Validators.required, Validators.email]],
            newPassword: ['', [Validators.required, passwordValidator]],
            passwordRepeat: ['', [Validators.required, passwordValidator]]
        }, {
            validators: passwordMatchValidator.bind(this)
        });

        this.friendService.onlineFriends$.subscribe(friends => {
            this.onlineFriends = friends;
        });
    };

    ngAfterViewChecked(): void {
        this.scrollContainer.nativeElement.scrollTop = this.scrollContainer.nativeElement.scrollHeight;
    };

    ngOnDestroy(): void {
        this.subscriptions.unsubscribe();

        if (this.routeSub) {
            this.routeSub.unsubscribe();
        }
      };

    loadAvatarsFromMessages(userId : string) {
        if (userId === null || userId === undefined) {
            return;
        }

        if (this.avatars[userId] == null) {
            this.mediaService.getAvatarImage(userId).subscribe(
                (avatar) => {
                    this.avatars[userId] = avatar;
                })
        }
    };

    async sendMessage() {
        const decryptedRoomKey = await this.cryptoService.getDecryptedRoomKey(this.userKey!, this.roomId);

        if (decryptedRoomKey === null) {
            console.error("Cannot get room key.");
        }

        const encryptedMessageData = await this.cryptoService.encryptMessage(this.inputMessage, decryptedRoomKey!);
                
            let request = new MessageRequest(
                this.roomId,
                encryptedMessageData.encryptedMessage,
                this.cookieService.get("Anonymous") === "True",
                encryptedMessageData.iv
            );
    
            this.saveMessage(request)
                .then((messageId) => {
                    request.messageId = messageId;
                    this.chatService.sendMessage(request);
                    this.inputMessage = "";
                }).catch((err: any) => {
                    console.log(err);
                })
    };

    async saveMessage(request: MessageRequest): Promise<string> {
        return new Promise<string>((resolve, reject) => {
            this.chatService.saveMessage(request)
                .subscribe((res: any) => {
                    this.chatService.messages[this.roomId].push({
                        encrypted: true,
                        messageData: {
                            roomId: res.data.roomId,
                            messageId: res.data.messageId,
                            userId: res.data.senderId,
                            message: res.data.text,
                            messageTime: res.data.sendTime,
                            seenList: res.data.seen,
                            iv: res.data.iv
                        }
                    });

                    resolve(res.data.messageId);
                },
                (error) => {
                    reject(error);
                });
        });
    };

    leaveChat(deleted: boolean) {
        this.chatService.leaveChat()
        .then(() => {
            if (deleted) {
                this.router.navigate(['/join-room'])
            };
        }).catch((err) => {
            console.log(err);
        })
    };

    async getMessages() {
        if (!this.userKey) {
            this.userKey = await this.dbService.getEncryptionKey(this.userId);
            if (!this.userKey) {
                console.error("Failed to load userKey");
                return;
            }
        }
    
        if (this.chatService.messages[this.roomId] === undefined) {
            return;
        }
    
        const decryptedRoomKey = await this.cryptoService.getDecryptedRoomKey(this.userKey, this.roomId);
        if (decryptedRoomKey === null) {
            console.error("Cannot get room key.");
            return;
        }

        const request: GetMessagesRequest = {
            roomId: this.roomId,
            index: 1
        }
    
        this.chatService.getMessages(request).subscribe((messagesResponse: ServerResponse<ReceiveMessageResponse[]>) => {
            if (!messagesResponse.isSuccess) {
                return;
            }
            
            const userNames = messagesResponse.data.map((element: any) =>
                this.userService.getUsername(element.senderId)
            );
    
            forkJoin(userNames).subscribe(async (userNameData: any) => {
                const fetchedMessages = messagesResponse.data.map(async (element: any, index: number) => ({
                    encrypted: false,
                    messageData: {
                        messageId: element.messageId,
                        user: element.sentAsAnonymous === true ? "Anonymous" : userNameData[index].username,
                        userId: element.senderId,
                        message: await this.cryptoService.decryptMessage(element.text, decryptedRoomKey!, element.iv),
                        messageTime: element.sendTime,
                        seenList: element.seen
                    }
                }));
    
                const decryptedMessages = await Promise.all(fetchedMessages);
    
                const existingMessageIds = new Set(this.chatService.messages[this.roomId].map((msg: any) => msg.messageData.messageId));
                const uniqueMessages = decryptedMessages.filter((msg: any) => !existingMessageIds.has(msg.messageData.messageId));
    
                this.chatService.messages[this.roomId] = [...uniqueMessages, ...this.chatService.messages[this.roomId]];
                this.chatService.messages$.next(this.chatService.messages[this.roomId]);
            });
        });
    }

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

    handleMessageModify(messageId: string, messageText: string) {
        this.messageModifyBool = true;
        this.messageModifyRequest.id = messageId;
        this.inputMessage = messageText;
        this.inputElement.nativeElement.focus();
    };

    async sendMessageModifyHttpRequest(request: ChangeMessageRequest) {
        const decryptedRoomKey = await this.cryptoService.getDecryptedRoomKey(this.userKey!, this.roomId);

        if (decryptedRoomKey === null) {
            console.error("Cannot get room key.");
        }

        const encryptedData = await this.cryptoService.encryptMessage(this.inputMessage, decryptedRoomKey!);
        this.chatService.editMessage(request)
                .subscribe(() => {
                    this.chatService.messages[this.roomId].forEach((message: any) => {
                        if (message.messageData.messageId == request.id) {
                            message.encrypted = true;
                            message.messageData.iv = encryptedData.iv;
                            this.chatService.modifyMessage(request);
                            this.inputMessage = "";
                            this.messageModifyBool = false;
                        }
                    })
                },
                (error) => {
                    if (error.status === 400) {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: 'Something unusual happened.'
                        });
                    } else {
                        console.error("An error occurred:", error);
                    }
                });
    };

    handleCloseMessageModify() {
        this.inputMessage = "";
        this.messageModifyBool = false;
    };

    sendMessageSeenModifyHttpRequest(request: ChangeMessageSeenRequest) {
        this.chatService.editMessageSeen(request)
        .subscribe(() => {
            this.chatService.messages[this.roomId].forEach((message: any) => {
                if (message.messageId == request.userId) {
                    this.inputMessage = "";
                    this.messageModifyBool = false;
                }
            })
        },
        (error) => {
            if (error.status === 400) {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'Something unusual happened.'
                });
            } else {
                console.error("An error occurred:", error);
            }
        });
    };

    handleMessageDelete(messageId: any) {
        this.chatService.messageDelete(messageId)
        .subscribe(() => {
            this.chatService.messages[this.roomId].forEach((message: any) => {
                if (message.messageData.messageId == messageId) {
                    this.chatService.deleteMessage(messageId);
                }
            })
        },
        (error) => {
            if (error.status === 400) {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'Something unusual happened.'
                });
            } else {
                console.error("An error occurred:", error);
            }
        });
    };

    @HostListener('window:focus', ['$event'])
    onFocus(): void {
        this.chatService.messages[this.roomId].forEach((message) => {
            const userId = this.userId;
            const anonym = this.cookieService.get("Anonymous") == "True";
            if (!message.seenList) {
                return;
            } else if (!message.seenList.includes(userId)) {
                var request = new ChangeMessageSeenRequest(userId, anonym, message.messageId);
                this.chatService.modifyMessageSeen(request);
                this.sendMessageSeenModifyHttpRequest(request);
            }
        })
    };

    examineIfNextMessageNotContainsUserId(userId: string, index: number) {
        const slicedMessages = this.chatService.messages[this.roomId].slice(index + 1);

        for (const message of slicedMessages) {
            if (message.seenList == null) {
                continue;
            }

            if (message.seenList.includes(userId)) {
                return false;
            }
        }

        this.imageCount++;
        return true;
    };

    resetImageCount() {
        this.imageCount = 0;
    };

    userIsTheCreatorMethod(){
        this.chatService.userIsTheCreator(this.roomId)
        .subscribe((result) => {
            if (result) {
                this.userIsTheCreator = true;
            }
        });
    }

    deleteRoom() {
        this.isLoading = true;
        this.chatService.deleteRoom(this.roomId).then(() => {
            this.chatService.deleteRoomHttpRequest(this.roomId)
            .subscribe((response: any) => {
                if (response) {
                    this.isLoading = false;
                    this.leaveChat(false);
                    this.router.navigate(['/join-room'], { queryParams: { deleteSuccess: 'true' } });
                } else {
                    this.isLoading = false;
                }
            },
            (error) => {
                if (error.status === 400) {
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Error',
                        detail: 'Something unusual happened.'
                    });
                } else {
                    console.error("An error occurred:", error);
                }
            });
        })
    }

    toggleShowPassword() {
        this.showPassword = !this.showPassword;
    }

    changePasswordForRoom() {
        const changePasswordRequest: ChangePasswordForRoomRequest = {
            id: this.roomId,
            oldPassword: this.changePasswordRequest.get('oldPassword')?.value,
            password: this.changePasswordRequest.get('newPassword')?.value
        };

            this.chatService.changePasswordForRoom(changePasswordRequest)
            .subscribe((response: any) => {
                if (response.success) {
                    this.messageService.add({
                        severity: 'success',
                        summary: 'Success',
                        detail: 'Password successfully updated.',
                        styleClass: 'ui-toast-message-success'
                    });
                }
            },
            (error) => {
                if (error.status === 400) {
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Error',
                        detail: 'Wrong password.'
                    });
                } else {
                    console.error("An error occurred:", error);
                }
            });
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

    async handleInviteToRoom(receiverName: string) {
        let userName = "";
        this.connectedUsers.forEach(user => {
            if (user.userId == this.userId) {
                userName = user.userName
            }
        })

        await firstValueFrom(this.cryptoService.userHaveKeyForRoom(receiverName, sessionStorage.getItem("roomId")!))
        .then(() => {
            var request: ChatRoomInviteRequest = {
                roomId: sessionStorage.getItem("roomId")!,
                roomName: sessionStorage.getItem("room")!,
                receiverName: receiverName,
                senderId: this.userId!,
                senderName: userName
                
            }
            this.friendService.sendChatRoomInvite(request);
        })
            .catch(async err => {
                if (err.error !== `There is no key or user with this Username: ${receiverName}`) {
                    return;
                }

                const receiverObject = await firstValueFrom(this.cryptoService.getPublicKey(receiverName));
                if (receiverObject.isSuccess) {
                    const decryptedRoomKey = await this.cryptoService.getDecryptedRoomKey(this.userKey!, this.roomId);

                    if (decryptedRoomKey === null) {
                        console.error("Cannot get room key.");
                    }

                    const keyToArrayBuffer = await this.cryptoService.exportCryptoKey(decryptedRoomKey!);
                    const receiverPublicKey = receiverObject.message;
                    const cryptoKeyUserPublicKey = await this.cryptoService.importPublicKeyFromBase64(receiverPublicKey);
                    const encryptRoomKeyForUser = await this.cryptoService.encryptSymmetricKey(keyToArrayBuffer, cryptoKeyUserPublicKey);
                    const bufferToBase64 = this.cryptoService.bufferToBase64(encryptRoomKeyForUser);
    
                    var request: ChatRoomInviteRequest = {
                        roomId: sessionStorage.getItem("roomId")!,
                        roomName: sessionStorage.getItem("room")!,
                        receiverName: receiverName,
                        senderId: this.userId!,
                        senderName: userName,
                        roomKey: bufferToBase64
                    }
                    this.friendService.sendChatRoomInvite(request)
                }
            });


        this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: `Room invite successfully sent to ${receiverName}.`, styleClass: 'ui-toast-message-success'
        });
    }
}
