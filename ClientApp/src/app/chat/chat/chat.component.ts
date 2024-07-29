import { AfterViewChecked, ChangeDetectionStrategy, Component, ElementRef, HostListener, OnInit, ViewChild } from '@angular/core';
import { ChatService } from '../../services/chat-service/chat.service';
import { Router } from '@angular/router';
import { firstValueFrom, forkJoin, Subscription } from 'rxjs';
import { filter, take } from 'rxjs/operators';
import { MessageRequest } from '../../model/message-requests/MessageRequest';
import { CookieService } from 'ngx-cookie-service';
import { ChangeMessageRequest } from '../../model/user-credential-requests/ChangeMessageRequest';
import { ChangeMessageSeenRequest } from '../../model/message-requests/ChangeMessageSeenRequest';
import { ConnectedUser } from '../../model/room-requests/ConnectedUser';
import { MessageService } from 'primeng/api';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { passwordMatchValidator, passwordValidator } from '../../validators/ValidPasswordValidator';
import { FriendService } from '../../services/friend-service/friend.service';
import { FriendRequestManage } from '../../model/friend-requests/FriendRequestManage';
import { DisplayService } from '../../services/display-service/display.service';
import { MediaService } from '../../services/media-service/media.service';
import { ChangePasswordRequestForRoom } from '../../model/room-requests/ChangePasswordRequestForRoom';
import { UserService } from '../../services/user-service/user.service';
import { CryptoService } from '../../services/crypto-service/crypto.service';
import { IndexedDBService } from '../../services/db-service/indexed-dbservice.service';

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
    myImage: string = "./assets/images/chat-mountain.jpg";
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
    onlineFriends: FriendRequestManage[] | undefined;
    userKey: string = "";

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

        this.dbService.getEncryptionKey(this.userId).then(key => {
            if (key !== null) {
                this.userKey = key;
            }
        })

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

        this.chatService.messages$.subscribe(async data => {
            const userEncryptedData = await firstValueFrom(this.cryptoService.getUserPrivateKeyAndIv());
            const encryptedRoomSymmetricKey = await firstValueFrom(this.cryptoService.getUserPrivateKeyForRoom(this.roomId));
            const encryptedRoomSymmetricKeyToArrayBuffer = this.cryptoService.base64ToBuffer(encryptedRoomSymmetricKey.encryptedKey);
            const decryptedUserPrivateKey = await this.cryptoService.decryptPrivateKey(userEncryptedData.encryptedPrivateKey, this.userKey, userEncryptedData.iv);
            const decryptedUserCryptoPrivateKey = await this.cryptoService.importPrivateKeyFromBase64(decryptedUserPrivateKey!);
            const decryptedRoomKey = await this.cryptoService.decryptSymmetricKey(encryptedRoomSymmetricKeyToArrayBuffer, decryptedUserCryptoPrivateKey);

            const decryptedMessages = await Promise.all(data.map(async innerData => {
                if (innerData.encrypted) {
                    innerData.messageData.message = await this.cryptoService.decryptMessage(innerData.messageData.message, decryptedRoomKey, innerData.messageData.iv);
                    innerData.encrypted = false;
                    return innerData;
                } else {
                    return innerData;
                }
            }));

            this.messages = decryptedMessages;

            this.messages.forEach(message => {
                this.mediaService.getAvatarImage(this.userId).subscribe((image) => {
                    this.avatars[this.userId] = image;
                });
                
                setTimeout(() => {
                    if (message.messageData.userId == undefined) {
                        const currentIndex = this.messages.indexOf(message);

                        if (currentIndex > -1) {
                            this.messages.splice(currentIndex, 1);
                        }
                    }
                }, 5000);
            })
        });

        this.chatService.connection.on("ModifyMessage", async (messageId: string, messageText: string) => {
            const userEncryptedData = await firstValueFrom(this.cryptoService.getUserPrivateKeyAndIv());
            const encryptedRoomSymmetricKey = await firstValueFrom(this.cryptoService.getUserPrivateKeyForRoom(this.roomId));
            const encryptedRoomSymmetricKeyToArrayBuffer = this.cryptoService.base64ToBuffer(encryptedRoomSymmetricKey.encryptedKey);
            const decryptedUserPrivateKey = await this.cryptoService.decryptPrivateKey(userEncryptedData.encryptedPrivateKey, this.userKey, userEncryptedData.iv);
            const decryptedUserCryptoPrivateKey = await this.cryptoService.importPrivateKeyFromBase64(decryptedUserPrivateKey!);
            const decryptedRoomKey = await this.cryptoService.decryptSymmetricKey(encryptedRoomSymmetricKeyToArrayBuffer, decryptedUserCryptoPrivateKey);

            this.messages.forEach(async (data) => {
                if (data.messageData.messageId == messageId && data.encrypted) {
                    console.log(data.messageData.iv);
                    data.messageData.message = await this.cryptoService.decryptMessage(messageText, decryptedRoomKey, data.messageData.iv);
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
        })
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
        const userEncryptedData = await firstValueFrom(this.cryptoService.getUserPrivateKeyAndIv());
        const encryptedRoomSymmetricKey = await firstValueFrom(this.cryptoService.getUserPrivateKeyForRoom(this.roomId));
        const encryptedRoomSymmetricKeyToArrayBuffer = this.cryptoService.base64ToBuffer(encryptedRoomSymmetricKey.encryptedKey);
        const decryptedUserPrivateKey = await this.cryptoService.decryptPrivateKey(userEncryptedData.encryptedPrivateKey, this.userKey, userEncryptedData.iv);
        const decryptedUserCryptoPrivateKey = await this.cryptoService.importPrivateKeyFromBase64(decryptedUserPrivateKey!);
        const decryptedRoomKey = await this.cryptoService.decryptSymmetricKey(encryptedRoomSymmetricKeyToArrayBuffer, decryptedUserCryptoPrivateKey);
        const encryptedMessageData = await this.cryptoService.encryptMessage(this.inputMessage, decryptedRoomKey);
            
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
                            roomId: res.message.roomId,
                            messageId: res.message.messageId,
                            userId: res.message.senderId,
                            message: res.message.text,
                            messageTime: res.message.sendTime,
                            seenList: res.message.seen,
                            iv: res.message.iv
                        }
                    });

                    resolve(res.message.messageId);
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
        if (this.chatService.messages[this.roomId] === undefined) {
            return;
        }

        const userEncryptedData = await firstValueFrom(this.cryptoService.getUserPrivateKeyAndIv());
        const encryptedRoomSymmetricKey = await firstValueFrom(this.cryptoService.getUserPrivateKeyForRoom(this.roomId));
        const encryptedRoomSymmetricKeyToArrayBuffer = this.cryptoService.base64ToBuffer(encryptedRoomSymmetricKey.encryptedKey);
        const decryptedUserPrivateKey = await this.cryptoService.decryptPrivateKey(userEncryptedData.encryptedPrivateKey, this.userKey, userEncryptedData.iv);
        if (decryptedUserPrivateKey === null) {
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: "The provided user key is wrong. You can change the key under: 'your avatar -> profile -> Change User key' section."
            });
        }
        
        const decryptedUserCryptoPrivateKey = await this.cryptoService.importPrivateKeyFromBase64(decryptedUserPrivateKey!);
        const decryptedRoomKey = await this.cryptoService.decryptSymmetricKey(encryptedRoomSymmetricKeyToArrayBuffer, decryptedUserCryptoPrivateKey);
    
        this.chatService.getMessages(this.roomId)
            .subscribe((response: any) => {
                const userNames = response.map((element: any) =>
                    this.userService.getUsername(element.senderId)
                );
    
                forkJoin(userNames).subscribe(async (usernames: any) => {
                    const fetchedMessages = response.map(async (element: any, index: number) => ({
                        encrypted: false,
                        messageData: {
                            messageId: element.messageId,
                            user: element.sentAsAnonymous === true ? "Anonymous" : usernames[index].username,
                            userId: element.senderId,
                            message: await this.cryptoService.decryptMessage(element.text, decryptedRoomKey, element.iv),
                            messageTime: element.sendTime,
                            seenList: element.seen
                        }
                    }));
                
                    const decryptedMessages = await Promise.all(fetchedMessages);
                
                    const existingMessageIds = new Set(this.chatService.messages[this.roomId].map((msg: any) => msg.messageData.messageId));
                    const uniqueMessages = decryptedMessages.filter((msg: any) => !existingMessageIds.has(msg.messageId));
                
                    this.chatService.messages[this.roomId] = [...uniqueMessages, ...this.chatService.messages[this.roomId]];
                
                    this.chatService.messages$.next(this.chatService.messages[this.roomId]);
                });
            });
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

    handleMessageModify(messageId: string, messageText: string) {
        this.messageModifyBool = true;
        this.messageModifyRequest.id = messageId;
        this.inputMessage = messageText;
        this.inputElement.nativeElement.focus();
    };

    async sendMessageModifyHttpRequest(request: ChangeMessageRequest) {
        const userEncryptedData = await firstValueFrom(this.cryptoService.getUserPrivateKeyAndIv());
        const encryptedRoomSymmetricKey = await firstValueFrom(this.cryptoService.getUserPrivateKeyForRoom(this.roomId));
        const encryptedRoomSymmetricKeyToArrayBuffer = this.cryptoService.base64ToBuffer(encryptedRoomSymmetricKey.encryptedKey);
        const decryptedUserPrivateKey = await this.cryptoService.decryptPrivateKey(userEncryptedData.encryptedPrivateKey, this.userKey, userEncryptedData.iv);
        const decryptedUserCryptoPrivateKey = await this.cryptoService.importPrivateKeyFromBase64(decryptedUserPrivateKey!);
        const decryptedRoomKey = await this.cryptoService.decryptSymmetricKey(encryptedRoomSymmetricKeyToArrayBuffer, decryptedUserCryptoPrivateKey);
        const encryptedData = await this.cryptoService.encryptMessage(this.inputMessage, decryptedRoomKey);

        request.message = encryptedData.encryptedMessage;
        request.iv = encryptedData.iv;

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
        .subscribe((result: boolean) => {
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
        const changePasswordRequest = new ChangePasswordRequestForRoom(
            this.roomId,
            this.changePasswordRequest.get('oldPassword')?.value,
            this.changePasswordRequest.get('newPassword')?.value
            );
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
            this.friendService.sendChatRoomInvite(
                sessionStorage.getItem("roomId")!,
                sessionStorage.getItem("room")!,
                receiverName,
                this.userId!,
                userName
            )
        })
            .catch(async err => {
                if (err.error !== `There is no key or user with this Username: ${receiverName}`) {
                    return;
                }

                const receiverObject = await firstValueFrom(this.cryptoService.getPublicKey(receiverName));
                const receiverPublicKey = receiverObject.publicKey;

                const userEncryptionInput = await this.dbService.getEncryptionKey(this.cookieService.get("UserId"));
                const cryptoKeyUserPublicKey = await this.cryptoService.importPublicKeyFromBase64(receiverPublicKey);
                const userEncryptedData = await firstValueFrom(this.cryptoService.getUserPrivateKeyAndIv());
                const encryptedRoomSymmetricKey = await firstValueFrom(this.cryptoService.getUserPrivateKeyForRoom(sessionStorage.getItem("roomId")!));
                const encryptedRoomSymmetricKeyToArrayBuffer = this.cryptoService.base64ToBuffer(encryptedRoomSymmetricKey.encryptedKey);
                const decryptedUserPrivateKey = await this.cryptoService.decryptPrivateKey(userEncryptedData.encryptedPrivateKey, userEncryptionInput!, userEncryptedData.iv);
                const decryptedUserCryptoPrivateKey = await this.cryptoService.importPrivateKeyFromBase64(decryptedUserPrivateKey!);
                const decryptedRoomKey = await this.cryptoService.decryptSymmetricKey(encryptedRoomSymmetricKeyToArrayBuffer, decryptedUserCryptoPrivateKey);
                const keyToArrayBuffer = await this.cryptoService.exportCryptoKey(decryptedRoomKey);
                const encryptRoomKeyForUser = await this.cryptoService.encryptSymmetricKey(keyToArrayBuffer, cryptoKeyUserPublicKey);
                const bufferToBase64 = this.cryptoService.bufferToBase64(encryptRoomKeyForUser);

                this.friendService.sendChatRoomInvite(
                    sessionStorage.getItem("roomId")!,
                    sessionStorage.getItem("room")!,
                    receiverName,
                    this.userId!,
                    userName,
                    bufferToBase64
                )
            });


        this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: `Room invite successfully sent to ${receiverName}.`, styleClass: 'ui-toast-message-success'
        });
    }
}
