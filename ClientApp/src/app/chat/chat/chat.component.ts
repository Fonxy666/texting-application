import { AfterViewChecked, Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { ChatService } from '../../chat.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-chat',
  templateUrl: './chat.component.html',
  styleUrl: './chat.component.css'
})
export class ChatComponent implements OnInit, AfterViewChecked {

    inputMessage = "";
    loggedInUserName = sessionStorage.getItem("user");
    roomName = sessionStorage.getItem("room");
    myImage: string = "./assets/images/chat-mountain.jpg";

    @ViewChild('scrollMe') public scrollContainer!: ElementRef;

    constructor(public chatService: ChatService, public router: Router) { }
    
    messages: any[] = [];

    ngOnInit(): void {
        this.chatService.message$.subscribe(res => {
            this.messages = res;
            console.log(this.messages);
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
}
