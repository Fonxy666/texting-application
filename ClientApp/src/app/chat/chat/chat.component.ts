import { AfterViewChecked, ChangeDetectionStrategy, Component, ElementRef, HostListener, OnInit, ViewChild } from '@angular/core';
import { ChatService } from '../../services/chat-service/chat.service';
import { Router, ActivatedRoute  } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { Observable, of, forkJoin, Subscription  } from 'rxjs';
import { catchError, filter, switchMap, take } from 'rxjs/operators';
import { MessageRequest } from '../../model/MessageRequest';
import { ErrorHandlerService } from '../../services/error-handler.service';
import { CookieService } from 'ngx-cookie-service';
import { ChangeMessageRequest } from '../../model/ChangeMessageRequest';
import { ChangeMessageSeenRequest } from '../../model/ChangeMessageSeenRequest';
import { ConnectedUser } from '../../model/ConnectedUser';
import { MessageService } from 'primeng/api';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ChangePasswordRequest } from '../../model/ChangePasswordRequest';
import { passwordMatchValidator, passwordValidator } from '../../validators/ValidPasswordValidator';

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
    messageModifyBool: boolean = false;
    messageModifyRequest: ChangeMessageRequest = {id: "", message: ""};
    isPageVisible = true;
    imageCount: number = 0;
    userIsTheCreator: boolean = false;
    showPassword: boolean = false;
    isLoading: boolean = false;
    private subscriptions: Subscription = new Subscription();

    @ViewChild('scrollMe') public scrollContainer!: ElementRef;
    @ViewChild('messageInput') public inputElement!: ElementRef;

    private routeSub!: Subscription;

    constructor(public chatService: ChatService, public router: Router, private http: HttpClient, private route: ActivatedRoute, private errorHandler: ErrorHandlerService, private cookieService: CookieService, private messageService: MessageService, private fb: FormBuilder) { }
    
    messages: any[] = [];
    avatars: { [userId: string]: string } = {};
    changePasswordRequest!: FormGroup;

    ngOnInit(): void {
        this.userId = this.cookieService.get("UserId");
        this.roomId = sessionStorage.getItem("roomId")!;

        this.chatService.setCurrentRoom(this.roomId);

        if (this.roomId) {
            // Wait until the messages for the roomId are initialized
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

        this.chatService.message$.subscribe(res => {
            this.messages = res;
            this.messages.forEach(message => {
                this.loadAvatarsFromMessages(message.userId);
                setTimeout(() => {
                    if (message.userId == undefined) {
                        const currentIndex = this.messages.indexOf(message);

                        if (currentIndex > -1) {
                            this.messages.splice(currentIndex, 1);
                        }
                    }
                }, 5000);
            })
        });

        this.chatService.connection.on("ModifyMessage", (messageId: string, messageText: string) => {
            this.messages.forEach((message) => {
                if (message.messageId == messageId) {
                    message.message = messageText;
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
            this.messages.forEach((message: any) => {
                if (message.messageId == messageId) {
                    message.message = "Deleted message.";
                }
            });
        });
      
        this.chatService.connectedUsers.subscribe((users) => {
            this.connectedUsers = users;
            users.forEach((user) => {
                this.getAvatarImage(user.userId).subscribe(
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
                console.log(deletedRoomId);
                console.log(this.roomId);
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
            this.getAvatarImage(userId).subscribe(
                (avatar) => {
                    this.avatars[userId] = avatar;
                })
        }
    };

    sendMessage() {
        var request = new MessageRequest(this.roomId, this.userId, this.inputMessage, this.cookieService.get("Anonymous") === "True");
        this.saveMessage(request)
            .then((messageId) => {
                this.chatService.sendMessage(new MessageRequest(this.roomId, this.userId, this.inputMessage, this.cookieService.get("Anonymous") === "True", messageId));
                this.inputMessage = "";
            }).catch((err: any) => {
                console.log(err);
            })
    };

    saveMessage(request: MessageRequest): Promise<string> {
        return new Promise<string>((resolve, reject) => {
            this.http.post(`api/v1/Message/SendMessage`, request, { withCredentials: true})
                .pipe(
                    this.errorHandler.handleError401()
                )
                .subscribe((res: any) => {
                    this.chatService.messages[this.roomId].push({
                        roomId: res.roomId,
                        messageId: res.message.messageId,
                        userId: res.message.senderId,
                        message: res.message.text,
                        messageTime: res.message.sendTime,
                        seenList: res.message.seen
                    });
    
                    resolve(res.message.messageId);
                }, 
                (error) => {
                    if (error.status === 403) {
                        this.errorHandler.handleError403(error);
                    } else if (error.status === 400) {
                        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Something unusual happened.' });
                    } else {
                        console.error("An error occurred:", error);
                    }
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

    getMessages() {
        this.http.get(`/api/v1/Message/GetMessages/${this.roomId}`, { withCredentials: true })
            .pipe(
                this.errorHandler.handleError401()
            )
            .subscribe((response: any) => {
                const userNames = response.map((element: any) =>
                    this.http.get(`/api/v1/User/GetUsername?userId=${element.senderId}`, { withCredentials: true })
                );
    
                forkJoin(userNames).subscribe((usernames: any) => {
                    const fetchedMessages = response.map((element: any, index: number) => ({
                        messageId: element.messageId,
                        user: element.sentAsAnonymous === true ? "Anonymous" : usernames[index].username,
                        userId: element.senderId,
                        message: element.text,
                        messageTime: element.sendTime,                    
                        seenList: element.seen
                    }));
                    
                    this.chatService.messages[this.roomId] = [...fetchedMessages, ...this.chatService.messages[this.roomId]];
    
                    this.chatService.message$.next(this.chatService.messages[this.roomId]);
                });
            });
    };

    getAvatarImage(userId: string): Observable<string> {
        return this.http.get(`/api/v1/User/GetImage?userId=${userId}`, { withCredentials: true, responseType: 'blob' })
            .pipe(
                this.errorHandler.handleError401(),
                switchMap((response: Blob) => {
                    const reader = new FileReader();
                    const result$ = new Observable<string>((observer) => {
                        reader.onloadend = () => {
                            observer.next(reader.result as string);
                            observer.complete();
                        };
                    });
                    reader.readAsDataURL(response);
                    return result$;
                }),
                catchError((error) => {
                    console.log(error);
                    return of("https://ptetutorials.com/images/user-profile.png");
                })
            );
    };

    searchInConnectedUsers() {
        if (this.searchTerm.trim() === '') {
            this.chatService.connectedUsers.subscribe(users => {
                this.connectedUsers = users;
            });
        } else {
            this.connectedUsers = this.chatService.connectedUsers.value.filter(user => 
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

    sendMessageModifyHttpRequest(request: ChangeMessageRequest) {
        request.message = this.inputMessage;
        this.http.patch(`/api/v1/Message/EditMessage`, request, { withCredentials: true })
        .pipe(
            this.errorHandler.handleError401()
        )
        .subscribe(() => {
            this.chatService.messages[this.roomId].forEach((message: any) => {
                if (message.messageId == request.id) {
                    this.chatService.modifyMessage(request);
                    this.inputMessage = "";
                    this.messageModifyBool = false;
                }
            })
        },
        (error) => {
            if (error.status === 403) {
                this.errorHandler.handleError403(error);
            } else if (error.status === 400) {
                this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Something unusual happened.' });
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
        this.http.patch(`/api/v1/Message/EditMessageSeen`, request, { withCredentials: true })
        .pipe(
            this.errorHandler.handleError401()
        )
        .subscribe(() => {
            this.chatService.messages[this.roomId].forEach((message: any) => {
                if (message.messageId == request.userId) {
                    this.inputMessage = "";
                    this.messageModifyBool = false;
                }
            })
        },
        (error) => {
            if (error.status === 403) {
                this.errorHandler.handleError403(error);
            } else if (error.status === 400) {
                this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Something unusual happened.' });
            } else {
                console.error("An error occurred:", error);
            }
        });
    };

    handleMessageDelete(messageId: any) {
        this.http.delete(`/api/v1/Message/DeleteMessage?id=${messageId}`, { withCredentials: true})
        .pipe(
            this.errorHandler.handleError401()
        )
        .subscribe(() => {
            this.chatService.messages[this.roomId].forEach((message: any) => {
                if (message.messageId == messageId) {
                    this.chatService.deleteMessage(messageId);
                }
            })
        }, 
        (error) => {
            if (error.status === 403) {
                this.errorHandler.handleError403(error);
            } else if (error.status === 400) {
                this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Something unusual happened.' });
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
        const userId = this.userId;
        this.http.get(`/api/v1/Chat/ExamineIfTheUserIsTheCreator?userId=${userId}&roomId=${this.roomId}`, { withCredentials: true})
        .pipe(
            this.errorHandler.handleError401()
        )
        .subscribe((result: boolean) => {
            if (result) {
                this.userIsTheCreator = true;
            }
        }, 
        (error) => {
            if (error.status === 403) {
                this.errorHandler.handleError403(error);
            }
        });
    }

    deleteRoom() {
        this.isLoading = true;
        this.chatService.deleteRoom(this.roomId).then(() => {
            this.http.delete(`/api/v1/Chat/DeleteRoom?userId=${this.userId}&roomId=${this.roomId}`, { withCredentials: true})
            .pipe(
                this.errorHandler.handleError401()
            )
            .subscribe((response: any) => {
                if (response) {
                    setTimeout(() => {
                        this.isLoading = false;
                        this.leaveChat(false);
                        this.router.navigate(['/join-room'], { queryParams: { deleteSuccess: 'true' } });
                    }, 1000);
                } else {
                    this.isLoading = false;
                }
            }, 
            (error) => {
                if (error.status === 403) {
                    this.errorHandler.handleError403(error);
                } else if (error.status === 400) {
                    this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Something unusual happened.' });
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
        const changePasswordRequest = new ChangePasswordRequest(
            this.roomId,
            this.changePasswordRequest.get('oldPassword')?.value,
            this.changePasswordRequest.get('newPassword')?.value
            );
            this.http.patch(`/api/v1/Chat/ChangePasswordForRoom`, changePasswordRequest, { withCredentials: true})
            .pipe(
                this.errorHandler.handleError401()
            )
            .subscribe((response: any) => {
                if (response.success) {
                    this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Password successfully updated.', styleClass: 'ui-toast-message-success' });
                }
            }, 
            (error) => {
                if (error.status === 403) {
                    this.errorHandler.handleError403(error);
                } else if (error.status === 400) {
                    this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Wrong password.' });
                } else {
                    console.error("An error occurred:", error);
                }
            });
    }
}
