import { Component, HostListener, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { CookieService } from 'ngx-cookie-service';
import { ErrorHandlerService } from '../services/error-handler.service';
import { FriendService } from '../services/friend-service/friend.service';

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
        private friendService: FriendService
    ) {}

    isDropstart: boolean = true;
    announceNumber: number = 0;
    userId: string = "";

    ngOnInit(): void {
        this.userId = this.cookieService.get("UserId");

        this.isLoggedIn();
        this.loadProfileData();
        this.checkScreenSize();

        this.getAnnounceNumber();

        this.friendService.friendRequests$.subscribe(requests => {
            this.announceNumber = 0;
            requests.forEach(request => {
                if (request.senderId !== this.userId) {
                    this.announceNumber++;
                }
            })
        });
    }

    profilePic: string = "";

    isLoggedIn(): boolean {
        return this.cookieService.check('UserId');
    }

    loadProfileData() {        
        if (this.userId) {
            this.http.get(`/api/v1/User/GetImage?userId=${this.userId}`, { withCredentials: true, responseType: 'blob' })
            .pipe(
                this.errorHandler.handleError401()
            )
            .subscribe(
                (response: Blob) => {
                    const reader = new FileReader();
                    reader.onloadend = () => {
                        this.profilePic = reader.result as string;
                    };
                    reader.readAsDataURL(response);
                },
                (error) => {
                    if (error.status === 403) {
                        this.errorHandler.handleError403(error);
                    }
                    console.error(error);
                    console.log("There is no Avatar for this user.");
                    this.profilePic = "https://ptetutorials.com/images/user-profile.png";
                }
            );
        }
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
