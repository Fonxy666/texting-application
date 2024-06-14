import { Component, HostListener, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { CookieService } from 'ngx-cookie-service';
import { ErrorHandlerService } from '../services/error-handler.service';
import { FriendService } from '../services/friend-service/friend.service';
import { isEqual } from 'lodash';
import { MediaService } from '../services/media-service/media.service';

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
        private errorHandler: ErrorHandlerService,
        private friendService: FriendService,
        private mediaService: MediaService
    ) {}

    isDropstart: boolean = true;
    announceNumber: number = 0;
    userId: string = "";
    friendRequests: any[] = [];
    profilePic: string = "";

    ngOnInit(): void {
        this.userId = this.cookieService.get("UserId");

        this.isLoggedIn();
        this.checkScreenSize();

        this.getAnnounceNumber();

        this.friendService.friendRequests$.subscribe(requests => {
            this.announceNumber = requests.length;
        });

        this.mediaService.getAvatarImage(this.userId).subscribe((image) => {
            this.profilePic = image;
        });
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

    getAnnounceNumber() {
        if (this.userId) {
            this.http.get(`/api/v1/User/GetFriendRequestCount?userId=${this.userId}`, { withCredentials: true })
            .pipe(
                this.errorHandler.handleError401()
            )
            .subscribe(
                (response: any) => {
                    this.announceNumber = response;
                },
                (error) => {
                    if (error.status === 403) {
                        this.errorHandler.handleError403(error);
                    }
                }
            );
        }
    }
}
