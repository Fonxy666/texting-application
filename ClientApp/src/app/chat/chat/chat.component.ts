import { AfterViewChecked, ChangeDetectionStrategy, Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { ChatService } from '../../chat.service';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { Observable, of  } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';

@Component({
  selector: 'app-chat',
  templateUrl: './chat.component.html',
  styleUrl: './chat.component.css',
  changeDetection: ChangeDetectionStrategy.Default
})

export class ChatComponent implements OnInit, AfterViewChecked {

    inputMessage = "";
    loggedInUserName = sessionStorage.getItem("user");
    roomName = sessionStorage.getItem("room");
    myImage: string = "./assets/images/chat-mountain.jpg";

    @ViewChild('scrollMe') public scrollContainer!: ElementRef;

    constructor(public chatService: ChatService, public router: Router, private http: HttpClient) { }
    
    messages: any[] = [];
    avatars: { [username: string]: string } = {};

    ngOnInit(): void {
        this.chatService.message$.subscribe(res => {
            this.messages = res;
        });
      
        this.chatService.connectedUsers.subscribe((users) => {
            console.log('Connected users updated:', users);
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
    }

    ngAfterViewChecked(): void {
        this.scrollContainer.nativeElement.scrollTop = this.scrollContainer.nativeElement.scrollHeight;
    }

    sendMessage() {
        this.chatService.sendMessage(this.inputMessage)
        .then(() => {
            this.inputMessage ="";
        }).catch((err) => {
            console.log(err);
        })
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

    getAvatarImage(username: string): Observable<string> {
        if (!username) {
            return of("https://ptetutorials.com/images/user-profile.png");
        }
    
        return this.http.get(`http://localhost:5000/User/GetImage/${username}`, { responseType: 'blob' })
            .pipe(
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
