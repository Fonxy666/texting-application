import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CookieService } from 'ngx-cookie-service';
import { FormGroup, FormBuilder, Validators } from '@angular/forms';
import { NavigationEnd, Router } from '@angular/router';
import { DomSanitizer } from '@angular/platform-browser';
import { ImageCroppedEvent } from 'ngx-image-cropper';
import { MessageService } from 'primeng/api';
import { filter } from 'rxjs';
import { FriendService } from '../../services/friend-service/friend.service';
import { MediaService } from '../../services/media-service/media.service';
import { UserService } from '../../services/user-service/user.service';
import { ErrorHandlerService } from '../../services/error-handler-service/error-handler.service';

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
        private cookieService: CookieService,
        private fb: FormBuilder,
        private router: Router,
        private sanitizer: DomSanitizer,
        private userService: UserService,
        private friendService: FriendService,
        private mediaService: MediaService
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
        this.getUser();
        this.mediaService.getAvatarImage(this.userId).subscribe((image) => {
            this.user.image = image;
        });
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
            this.announceNumber = requests.length;
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

    getUser() {
        this.userService.getUserCredentials()
        .subscribe(response => {
            if (response) {
                this.user.name = response.userName;
                this.user.email = response.email;
                this.user.twoFactorEnabled = response.twoFactorEnabled;
                this.userService.setEmail(response.email);
            }
        })
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

    getAnnounceNumber() {
        this.userService.getFriendRequestCount()
        .subscribe(
            (response: any) => {
                this.announceNumber = response;
            }
        );
    }
}

