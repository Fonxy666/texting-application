import { AfterViewChecked, ChangeDetectionStrategy, Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { ChatService } from '../../chat.service';
import { Router, ActivatedRoute  } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { Observable, of, forkJoin  } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';
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

    @ViewChild('scrollMe') public scrollContainer!: ElementRef;

    constructor(public chatService: ChatService, public router: Router, private http: HttpClient, private route: ActivatedRoute, private errorHandler: ErrorHandlerService, private cookieService: CookieService) { }
    
    messages: any[] = [];
    avatars: { [userId: string]: string } = {};

    ngOnInit(): void {
        this.chatService.message$.subscribe(res => {
            this.messages = res;
        });

        this.route.params.subscribe(params => {
            this.roomId = params['id'];
        })
      
        this.chatService.connectedUsers.subscribe((users) => {
            users.forEach((user) => {
                this.loggedInUserId = this.cookieService.get("UserId");
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

    sendMessage() {
        var request = new MessageRequest(this.roomId, this.cookieService.get("UserId"), this.inputMessage, this.cookieService.get("Anonymous") === "True");
        this.chatService.sendMessage(request)
            .then(() => {
                this.inputMessage = "";
                console.log(request);
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
        .subscribe((response: any) => {
            if (response.success) {
                console.log("Message sent successfully");
            }
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
                    console.log(response);
                    const fetchedMessages = response.map((element: any, index: number) => ({
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
        if (!userName) {
            return of("https://ptetutorials.com/images/user-profile.png");
        }
    
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
}
