import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CookieService } from 'ngx-cookie-service';
import { FormGroup, FormBuilder, Validators } from '@angular/forms';
import { ChangePasswordRequest } from '../../model/ChangePasswordRequest';
import { Router } from '@angular/router';
import { ChangeEmailRequest } from '../../model/ChangeEmailRequest';
import { DomSanitizer } from '@angular/platform-browser';
import { ImageCroppedEvent } from 'ngx-image-cropper';
import { ChangeAvatarRequest } from '../../model/ChangeAvatarRequest';
import { ErrorHandlerService } from '../../services/error-handler.service';
import { MessageService } from 'primeng/api';

@Component({
    selector: 'app-profile',
    templateUrl: './profile.component.html',
    styleUrl: './profile.component.css',
    providers: [ MessageService ]
})

export class ProfileComponent implements OnInit {
    isLoading: boolean = false;
    profilePic: string = "";
    imageChangedEvent: any = '';
    croppedImage: any = '';
    myImage: string = "./assets/images/chat-mountain.jpg";
    user: { id: string, name: string, image: string, token: string, email: string, twoFactorEnabled: boolean } = { id: "", name: '', image: '', token: '', email: '', twoFactorEnabled: false };
    passwordChangeRequest!: FormGroup;

    constructor(private http: HttpClient, private cookieService: CookieService, private fb: FormBuilder, private router: Router, private sanitizer: DomSanitizer, private errorHandler: ErrorHandlerService, private messageService: MessageService) {
        this.user.id = this.cookieService.get('UserId');
    }

    ngOnInit(): void {
        this.isLoading = true;
        this.getUser(this.user.id);
        this.loadProfileData(this.user.id);
        this.passwordChangeRequest = this.fb.group({
            oldPassword: ['', Validators.required],
            newPassword: ['', Validators.required]
        })
        this.isLoading = false;
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

    changePassword(data: ChangePasswordRequest) {
        console.log(data);
        this.isLoading = true;
        data.id = this.user.id;
        this.http.patch(`/api/v1/User/ChangePassword`, data, { withCredentials: true})
        .pipe(
            this.errorHandler.handleError401()
        )
        .subscribe((response: any) => {
            if (response) {
                this.isLoading = false;
                this.getUser(this.user.id);
                this.messageService.add({ severity: 'info', summary: 'Info', detail: 'Your password changed.', styleClass: 'ui-toast-message-info' });
            }
        }, 
        (error) => {
            this.isLoading = false;
            if (error.status === 403) {
                this.errorHandler.handleError403(error);
            } else if (error.status === 400) {
                this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Unsuccessful change, wrong password(s).' });
            }
        });
    }

    changeEmail(data: ChangeEmailRequest) {
        if (data.newEmail === data.oldEmail) {
            this.messageService.add({ severity: 'error', summary: 'Error', detail: 'This is your actual e-mail. Try with another.' });
            return;
        }
        this.http.patch(`/api/v1/User/ChangeEmail`, data, { withCredentials: true})
        .pipe(
            this.errorHandler.handleError401()
        )
        .subscribe((response: any) => {
            if (response) {
                this.getUser(this.user.id);
                this.messageService.add({ severity: 'info', summary: 'Info', detail: 'Your e-mail changed.', styleClass: 'ui-toast-message-info' });
            }
        }, 
        (error) => {
            if (error.status === 403) {
                this.errorHandler.handleError403(error);
            } else if (error.status === 400) {
                this.messageService.add({ severity: 'error', summary: 'Error', detail: 'This new e-mail is already in use. Try with another.' });
            }
        })
    }
}

