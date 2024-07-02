import { Component, HostListener, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { CookieService } from 'ngx-cookie-service';
import { FriendService } from '../services/friend-service/friend.service';
import { MediaService } from '../services/media-service/media.service';
import { ChatRoomInvite } from '../model/room-requests/ChatRoomInvite';
import { ChatService } from '../services/chat-service/chat.service';

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
        public chatService: ChatService
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
    loggedIn: boolean = false;

    ngOnInit(): void {
        this.userId = this.cookieService.get("UserId");

        this.isLoggedIn();
        this.checkScreenSize();

        this.roomId = sessionStorage.getItem("roomId")!;
        this.roomName = sessionStorage.getItem("room")!;
    }


    isLoggedIn() {
        this.loggedIn = this.cookieService.check('UserId');
    }

    logout() {
        this.http.get(`/api/v1/Auth/Logout`, { withCredentials: true })
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
