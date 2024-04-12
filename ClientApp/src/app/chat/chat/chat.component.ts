import { AfterViewChecked, ChangeDetectionStrategy, Component, ElementRef, HostListener, OnInit, ViewChild } from '@angular/core';
import { ChatService } from '../../services/chat-service/chat.service';
import { Router, ActivatedRoute  } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { Observable, of, forkJoin  } from 'rxjs';
import { catchError, switchMap, tap } from 'rxjs/operators';
import { MessageRequest } from '../../model/MessageRequest';
import { ErrorHandlerService } from '../../services/error-handler.service';
import { CookieService } from 'ngx-cookie-service';
import { ChangeMessageRequest } from '../../model/ChangeMessageRequest';
import { ChangeMessageSeenRequest } from '../../model/ChangeMessageSeenRequest';
import { ConnectedUser } from '../../model/ConnectedUser';

@Component({
  selector: 'app-chat',
  templateUrl: './chat.component.html',
  styleUrl: './chat.component.css',
  changeDetection: ChangeDetectionStrategy.Default
})

export class ChatComponent implements OnInit, AfterViewChecked {
    roomId = "";
    inputMessage = "";
    loggedInUserId:string = "";
    roomName = sessionStorage.getItem("room");
    myImage: string = "./assets/images/chat-mountain.jpg";
    connectedUsers: ConnectedUser[] = [];
    searchTerm: string = '';
    messageModifyBool: boolean = false;
    messageModifyRequest: ChangeMessageRequest = {id: "", message: ""};
    isPageVisible = true;

    @ViewChild('scrollMe') public scrollContainer!: ElementRef;
    @ViewChild('messageInput') public inputElement!: ElementRef;

    constructor(public chatService: ChatService, public router: Router, private http: HttpClient, private route: ActivatedRoute, private errorHandler: ErrorHandlerService, private cookieService: CookieService) { }
    
    messages: any[] = [];
    avatars: { [userId: string]: string } = {};

    ngOnInit(): void {
        this.loggedInUserId = this.cookieService.get("UserId");
        this.chatService.message$.subscribe(res => {
            console.log(res);
            this.messages = res;
            this.messages.forEach(message => {
                this.loadAvatarsFromMessages(message.userId);
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
            this.chatService.messages.forEach((message) => {
                if (!message.seenList) {
                    return;
                } else if (!message.seenList.includes(userIdFromSignalR)) {
                    message.seenList.push(userIdFromSignalR);
                }
            })
        })

        this.chatService.connection.on("DeleteMessage", (messageId: string) => {
            this.messages.forEach((message: any) => {
                if (message.messageId == messageId) {
                    message.message = "Deleted message.";
                }
            });
        });

        this.route.params.subscribe(params => {
            this.roomId = params['id'];
        })
      
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

        this.getMessages();
    }

    ngAfterViewChecked(): void {
        this.scrollContainer.nativeElement.scrollTop = this.scrollContainer.nativeElement.scrollHeight;
    }

    examineMessages() {
        this.chatService.messages.forEach((message) => {
            if (message.userId != this.cookieService.get("UserId")) {
                console.log(message.message);
            }
        })
    }

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

        // this.http.get(`https://localhost:7045/User/getUsername/${userId}`, { withCredentials: true })
        // .subscribe((user: any) => {
        //     if (this.avatars[user.username] == null) {
        //         this.getAvatarImage(user.username).subscribe(
        //             (avatar) => {
        //                 this.avatars[userId] = avatar;
        //             },
        //             (error) => {
        //                 console.log(error);
        //             }
        //         );
        //     }
        // });
    }

    sendMessage() {
        var request = new MessageRequest(this.roomId, this.cookieService.get("UserId"), this.inputMessage, this.cookieService.get("Anonymous") === "True");
        this.saveMessage(request)
            .then((messageId) => {
                this.chatService.sendMessage(new MessageRequest(this.roomId, this.cookieService.get("UserId"), this.inputMessage, this.cookieService.get("Anonymous") === "True", messageId));
                this.inputMessage = "";
            }).catch((err: any) => {
                console.log(err);
            })
    }

    saveMessage(request: MessageRequest): Promise<string> {
        return new Promise<string>((resolve, reject) => {
            this.http.post('https://localhost:7045/Message/SendMessage', request, { withCredentials: true})
                .pipe(
                    this.errorHandler.handleError401()
                )
                .subscribe((res: any) => {
                    this.chatService.messages.push({ 
                        messageId: res.message.messageId,
                        userId: res.message.senderId,
                        message: res.message.text,
                        messageTime: res.message.sendTime
                    });
    
                    resolve(res.message.messageId);
                }, 
                (error) => {
                    if (error.status === 403) {
                        this.errorHandler.handleError403(error);
                    } else if (error.status === 400) {
                        this.errorHandler.errorAlert("You cannot send empty messages.");
                    } else {
                        console.error("An error occurred:", error);
                    }
                    reject(error);
                });
        });
    }

    leaveChat() {
        this.chatService.leaveChat()
        .then(() => {
            this.router.navigate(['/join-room']);
            setTimeout(() => {
                location.reload();
            }, 0);
        }).catch((err) => {
            console.log(err);
        })
    }

    getMessages() {
        this.http.get(`https://localhost:7045/Message/GetMessages/${this.roomId}`, { withCredentials: true })
            .pipe(
                this.errorHandler.handleError401()
            )
            .subscribe((response: any) => {
                const observables = response.map((element: any) =>
                    this.http.get(`https://localhost:7045/User/getUsername/${element.senderId}`, { withCredentials: true })
                );
    
                forkJoin(observables).subscribe((usernames: any) => {
                    const fetchedMessages = response.map((element: any, index: number) => ({
                        messageId: element.messageId,
                        user: element.sentAsAnonymous === true ? "Anonymous" : usernames[index].username,
                        userId: element.senderId,
                        message: element.text,
                        messageTime: element.sendTime,                    
                        seenList: element.seen
                    }));
                    this.chatService.messages = [...fetchedMessages, ...this.chatService.messages];
    
                    this.chatService.message$.next(this.chatService.messages);
                });
            });
    }

    getAvatarImage(userId: string): Observable<string> {
        return this.http.get(`https://localhost:7045/User/GetImage/${userId}`, { withCredentials: true, responseType: 'blob' })
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
    }

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
    }

    handleMessageModify(messageId: string, messageText: string) {
        this.messageModifyBool = true;
        this.messageModifyRequest.id = messageId;
        this.inputMessage = messageText;
        this.inputElement.nativeElement.focus();
    }

    sendMessageModifyHttpRequest(request: ChangeMessageRequest) {
        request.message = this.inputMessage;
        this.http.patch(`https://localhost:7045/Message/EditMessage`, request, { withCredentials: true })
        .pipe(
            this.errorHandler.handleError401()
        )
        .subscribe(() => {
            this.chatService.messages.forEach((message: any) => {
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
                this.errorHandler.errorAlert("Something unusual happened.");
            } else {
                console.error("An error occurred:", error);
            }
        });
    }

    handleCloseMessageModify() {
        this.inputMessage = "";
        this.messageModifyBool = false;
    }

    sendMessageSeenModifyHttpRequest(request: ChangeMessageSeenRequest) {
        this.http.patch(`https://localhost:7045/Message/EditMessageSeen`, request, { withCredentials: true })
        .pipe(
            this.errorHandler.handleError401()
        )
        .subscribe(() => {
            this.chatService.messages.forEach((message: any) => {
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
                this.errorHandler.errorAlert("Something unusual happened.");
            } else {
                console.error("An error occurred:", error);
            }
        });
    }

    handleMessageDelete(messageId: any) {
        this.http.delete(`https://localhost:7045/Message/DeleteMessage?id=${messageId}`, { withCredentials: true})
        .pipe(
            this.errorHandler.handleError401()
        )
        .subscribe(() => {
            this.chatService.messages.forEach((message: any) => {
                if (message.messageId == messageId) {
                    this.chatService.deleteMessage(messageId);
                }
            })
        }, 
        (error) => {
            if (error.status === 403) {
                this.errorHandler.handleError403(error);
            } else if (error.status === 400) {
                this.errorHandler.errorAlert("Something unusual happened.");
            } else {
                console.error("An error occurred:", error);
            }
        });
    }
    
    @HostListener('window:focus', ['$event'])
    onFocus(): void {
        this.chatService.messages.forEach((message) => {
            const userId = this.cookieService.get("UserId");
            const anonym = this.cookieService.get("Anonymous") == "True";
            if (!message.seenList) {
                return;
            } else if (!message.seenList.includes(userId)) {
                var request = new ChangeMessageSeenRequest(userId, anonym, message.messageId);
                this.chatService.modifyMessageSeen(request);
                this.sendMessageSeenModifyHttpRequest(request);
            }
        })
    }
}
