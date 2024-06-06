import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CookieService } from 'ngx-cookie-service';
import { FormGroup, FormBuilder, Validators } from '@angular/forms';
import { NavigationEnd, Router } from '@angular/router';
import { DomSanitizer } from '@angular/platform-browser';
import { ImageCroppedEvent } from 'ngx-image-cropper';
import { ChangeAvatarRequest } from '../../model/ChangeAvatarRequest';
import { ErrorHandlerService } from '../../services/error-handler.service';
import { MessageService } from 'primeng/api';
import { filter } from 'rxjs';
import { UserService } from '../../services/user.service';
import { FriendService } from '../../services/friend-service/friend.service';

@Component({
    selector: 'app-profile',
    templateUrl: './profile.component.html',
    styleUrl: './profile.component.css',
    providers: [ MessageService ]
})

export class ProfileComponent implements OnInit {
    activeRoute: string | undefined;
    isLoading: boolean = false;
    profilePic: string = "";
    imageChangedEvent: any = '';
    croppedImage: any = '';
    myImage: string = "./assets/images/chat-mountain.jpg";
    user: { id: string, name: string, image: string, token: string, email: string, twoFactorEnabled: boolean } = { id: "", name: '', image: '', token: '', email: '', twoFactorEnabled: false };
    passwordChangeRequest!: FormGroup;
    announceNumber: number = 0;
    friendRequests: any[] = [];
    userId: string = "";

    constructor(
        private http: HttpClient,
        private cookieService: CookieService,
        private fb: FormBuilder,
        private router: Router,
        private sanitizer: DomSanitizer,
        private errorHandler: ErrorHandlerService,
        private messageService: MessageService,
        private userService: UserService,
        private friendService: FriendService
    ) {
        this.user.id = this.cookieService.get('UserId');

        this.router.events.pipe(
            filter(event => event instanceof NavigationEnd)
        ).subscribe(() => {
            this.activeRoute = this.router.url;
        });
    }

    ngOnInit(): void {
        this.userId = this.cookieService.get("UserId");
        this.isLoading = true;
        this.getUser(this.user.id);
        this.loadProfileData(this.user.id);
        this.passwordChangeRequest = this.fb.group({
            oldPassword: ['', Validators.required],
            newPassword: ['', Validators.required]
        })
        this.isLoading = false;

        this.userService.email$.subscribe(email => {
            this.user.email = email;
        });

        this.getAnnounceNumber();

        this.friendService.friendRequests$.subscribe(requests => {
            this.friendRequests = requests;
            this.displayNewFriendRequests();
        });
    }

    isActive(route: string): boolean {
        return this.router.isActive(this.router.createUrlTree([route]), true);
    }

    handleButtonClick(route: string): void {
        if (this.isActive(route)) {
            this.router.navigate(['/profile/profile']);
        } else {
            this.router.navigate([route]);
        }
    }

    getUser(userId: string) {
        if (userId) {
            this.http.get(`/api/v1/User/GetUserCredentials?userId=${userId}`, { withCredentials: true })
            .pipe(
                this.errorHandler.handleError401()
            )
            .subscribe((response: any) => {
                if (response) {
                    this.user.name = response.userName;
                    this.user.email = response.email;
                    this.user.twoFactorEnabled = response.twoFactorEnabled;
                    this.userService.setEmail(response.email);
                }
            },
            (error) => {
                if (error.status === 403) {
                    this.errorHandler.handleError403(error);
                }
            });
        } else {
            console.error('Username parameter is null or undefined.');
        }
    }

    loadProfileData(userId: string) {
        if (userId) {
            this.http.get(`/api/v1/User/GetImage?userId=${userId}`, { withCredentials: true, responseType: 'blob' })
            .pipe(
                this.errorHandler.handleError401()
            )
            .subscribe(
                (response: any) => {
                    if (response instanceof Blob) {
                        const reader = new FileReader();
                        reader.onloadend = () => {
                            this.user.image = reader.result as string;
                        };
                        reader.readAsDataURL(response);
                    }
                },
                (error) => {
                    if (error.status === 403) {
                        this.errorHandler.handleError403(error);
                    }
                    console.log(error);
                    this.user.image = "https://ptetutorials.com/images/user-profile.png";
                }
            );
        }
    }

    fileChangeEvent(event: any): void {
        this.imageChangedEvent = event;
    }

    imageCropped(event: ImageCroppedEvent) {
        this.croppedImage = this.sanitizer.bypassSecurityTrustUrl(event.base64!!);
        this.onProfilePicChange(event);
    }

    async onProfilePicChange(event: ImageCroppedEvent) {
        const base64data = event.blob instanceof Blob ? await this.getBase64FromBlob(event.blob) : '';
        this.profilePic = base64data;
    }
    
    getBase64FromBlob(blob: Blob): Promise<string> {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.readAsDataURL(blob);
            reader.onloadend = () => resolve(reader.result as string);
            reader.onerror = (error) => reject(error);
        });
    }

    changeAvatar() {
        const request = new ChangeAvatarRequest(this.user.id, this.profilePic);
        this.http.patch(`/api/v1/User/ChangeAvatar`, request, { withCredentials: true})
        .pipe(
            this.errorHandler.handleError401()
        )
        .subscribe((response: any) => {
            if (response && response.status === 'Ok') {
                this.getUser(this.user.id);
                this.messageService.add({ severity: 'info', summary: 'Info', detail: 'Avatar change succeeded :)', styleClass: 'ui-toast-message-info' });
            }
        }, 
        (error) => {
            if (error.status === 403) {
                this.errorHandler.handleError403(error);
            }
        });
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

    private displayNewFriendRequests() {
        this.friendRequests.forEach(request => {
            this.announceNumber++;
        });
    }
}

