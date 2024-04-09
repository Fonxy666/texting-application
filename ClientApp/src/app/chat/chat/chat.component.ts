import { AfterViewChecked, ChangeDetectionStrategy, Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { ChatService } from '../../services/chat-service/chat.service';
import { Router, ActivatedRoute  } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { Observable, of, forkJoin  } from 'rxjs';
import { catchError, switchMap, tap } from 'rxjs/operators';
import { MessageRequest } from '../../model/MessageRequest';
import { ErrorHandlerService } from '../../services/error-handler.service';
import { CookieService } from 'ngx-cookie-service';

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
    connectedUsers: string[] = [];
    searchTerm: string = '';

    @ViewChild('scrollMe') public scrollContainer!: ElementRef;

    constructor(public chatService: ChatService, public router: Router, private http: HttpClient, private route: ActivatedRoute, private errorHandler: ErrorHandlerService, private cookieService: CookieService) { }
    
    messages: any[] = [];
    avatars: { [userId: string]: string } = {};

    ngOnInit(): void {
        this.loggedInUserId = this.cookieService.get("UserId");
        this.chatService.message$.subscribe(res => {
            this.messages = res;
            console.log(res);
            this.messages.forEach(message => {
                this.loadAvatarsFromMessages(message.userId);
            })
        });

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
                this.getAvatarImage(user).subscribe(
                    (avatar) => {
                        this.avatars[user] = avatar;
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

    loadAvatarsFromMessages(userId : string) {
        if (userId === null || userId === undefined) {
            return;
        }

        this.http.get(`https://localhost:7045/User/getUsername/${userId}`, { withCredentials: true })
        .subscribe((user: any) => {
            if (this.avatars[user.username] == null) {
                this.getAvatarImage(user.username).subscribe(
                    (avatar) => {
                        this.avatars[user.username] = avatar;
                    },
                    (error) => {
                        console.log(error);
                    }
                );
            }
        });
    }

    sendMessage() {
        var request = new MessageRequest(this.roomId, this.cookieService.get("UserId"), this.inputMessage, this.cookieService.get("Anonymous") === "True");
        this.chatService.sendMessage(request)
            .then(() => {
                this.inputMessage = "";
                this.saveMessage(request);
            }).catch((err) => {
                console.log(err);
            })
    }

    saveMessage(request: MessageRequest) {
        this.http.post('https://localhost:7045/Message/SendMessage', request, { withCredentials: true})
        .pipe(
            this.errorHandler.handleError401()
        )
        .subscribe((res) => {
            this.chatService.messages.push({ 
                messageId: res.message.messageId,
                userId: res.message.senderId,
                message: res.message.text,
                messageTime: res.message.sendTime
            });
        }, 
        (error) => {
            if (error.status === 403) {
                this.errorHandler.handleError403(error);
            } else if (error.status === 400) {
                this.errorHandler.errorAlert("Invalid username or password.");
            } else {
                console.error("An error occurred:", error);
            }
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
                        messageTime: element.sendTime
                    }));
                    this.chatService.messages = [...fetchedMessages, ...this.chatService.messages];
    
                    this.chatService.message$.next(this.chatService.messages);
                });
            });
    }

    getAvatarImage(userName: string): Observable<string> {
        return this.http.get(`https://localhost:7045/User/GetImageWithUsername/${userName}`, { withCredentials: true, responseType: 'blob' })
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
                user.toLowerCase().includes(this.searchTerm.toLowerCase())
            );
        }
    }

    handleMessageModify(messageId: any) {
        console.log(messageId);
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
}
