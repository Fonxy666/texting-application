import { Component, HostListener, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { CookieService } from 'ngx-cookie-service';
import { FriendService } from '../services/friend-service/friend.service';
import { MediaService } from '../services/media-service/media.service';
import { ChatRoomInvite } from '../model/ChatRoomInvite';
import { ChatService } from '../services/chat-service/chat.service';
import { UserService } from '../services/user-service/user.service';

@Component({
    selector: 'app-nav-bar',
    templateUrl: './nav-bar.component.html',
    styleUrls: ['./nav-bar.component.css', '../../styles.css']
})

export class NavBarComponent implements OnInit {
    constructor(
        private cookieService : CookieService,
        private router: Router,
        private http: HttpClient,
        public friendService: FriendService,
        private mediaService: MediaService,
        public chatService: ChatService,
        private userService: UserService
    ) {}

    isDropstart: boolean = true;
    announceNumber: number = 0;
    announceNumberForInvite: number = 0;
    userId: string = "";
    friendRequests: any[] = [];
    profilePic: string = "";
    public chatRoomInvites: ChatRoomInvite[] = [];
    roomId: string = "";
    roomName: string = "";

    ngOnInit(): void {
        this.userId = this.cookieService.get("UserId");

        this.isLoggedIn();
        this.checkScreenSize();

        this.friendService.friendRequests$.subscribe(requests => {
            this.announceNumber = requests.length;
        });

        this.mediaService.getAvatarImage(this.userId).subscribe((image) => {
            this.profilePic = image;
        });

        this.friendService.chatRoomInvites$.subscribe(requests => {
            this.announceNumberForInvite = requests.length;
            this.chatRoomInvites = requests;
        })

        this.roomId = sessionStorage.getItem("roomId")!;
        this.roomName = sessionStorage.getItem("room")!;
    }


    isLoggedIn(): boolean {
        return this.cookieService.check('UserId');
    }

    logout() {
        var userId = this.cookieService.get('UserId');
        this.http.get(`/api/v1/Auth/Logout?userId=${userId}`, { withCredentials: true })
        .subscribe((response: any) => {
            if (response.success) {
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
}
