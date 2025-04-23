import { Component, HostListener, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { CookieService } from 'ngx-cookie-service';
import { FriendService } from '../services/friend-service/friend.service';
import { MediaService } from '../services/media-service/media.service';
import { ChatRoomInvite } from '../model/room-requests/ChatRoomInvite';
import { ChatService } from '../services/chat-service/chat.service';
import { IndexedDBService } from '../services/db-service/indexed-dbservice.service';
import { MessageService } from 'primeng/api';
import { AuthService } from '../services/auth-service/auth.service';

@Component({
    selector: 'app-nav-bar',
    templateUrl: './nav-bar.component.html',
    styleUrls: ['./nav-bar.component.css', '../../styles.css'],
    providers: [ MessageService ]
})

export class NavBarComponent implements OnInit {
    constructor(
        private cookieService : CookieService,
        private router: Router,
        private http: HttpClient,
        public friendService: FriendService,
        private mediaService: MediaService,
        public chatService: ChatService,
        private dbService: IndexedDBService,
        private messageService: MessageService,
        private authService: AuthService
    ) { }

    isDropstart: boolean = true;
    announceNumber: number = 0;
    announceNumberForInvite: number = 0;
    userId: string = "";
    friendRequests: any[] = [];
    profilePic: string = "";
    public chatRoomInvites: ChatRoomInvite[] = [];
    roomId: string = "";
    roomName: string = "";
    loggedIn: boolean = false;

    ngOnInit(): void {
        
        this.isLoggedIn();
        this.checkScreenSize();
        
        this.userId = this.cookieService.get("UserId");

        if (this.loggedIn) {
            this.friendService.friendRequests$.subscribe(requests => {
                this.announceNumber = requests.filter(request => {
                    return request.receiverId == this.userId;
                }).length;
            });
    
            this.mediaService.getAvatarImage(this.userId).subscribe(image =>
                this.profilePic = image
            );
    
            this.friendService.chatRoomInvites$.subscribe(requests => {
                this.announceNumberForInvite = requests.length;
                this.chatRoomInvites = requests;
            })
        }
    }


    isLoggedIn() {
        this.loggedIn = this.cookieService.check('UserId');
    }

    logout() {
        this.authService.logout()
        .subscribe((response: any) => {
            if (response.isSuccess) {
                this.dbService.clearEncryptionKey(this.userId);
                this.loggedIn = false;
                sessionStorage.clear();
                this.router.navigate(['/'], { queryParams: { logout: 'true' } });
            }
        }, 
        (error) => {
            console.error("An error occurred:", error);
        });
    }

    @HostListener('window:resize', ['$event'])
    onResize() {
        this.checkScreenSize();
    }

    checkScreenSize() {
        if (window.innerWidth <= 992) {
            this.isDropstart = false;
        } else {
            this.isDropstart = true;
        }
    }

    isCurrentRoute(routerLink: string): boolean {
        return this.router.url === routerLink;
    }

    examineIfUserIsInARoom(roomName: string, roomId: string, senderId: string) {
        if (this.chatService.userInRoom()) {
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: 'First you need to leave the actual room.'
            });
        }

        this.chatService.setRoomCredentialsAndNavigate(roomName, roomId, senderId);
    }
}
