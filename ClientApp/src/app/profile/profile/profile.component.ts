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

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.css'
})

export class ProfileComponent implements OnInit {

    profilePic: string = "";
    imageChangedEvent: any = '';
    croppedImage: any = '';
    myImage: string = "./assets/images/chat-mountain.jpg";
    user: { id: string, name: string, image: string, token: string, email: string, twoFactorEnabled: boolean } = { id: "", name: '', image: '', token: '', email: '', twoFactorEnabled: false };
    passwordChangeRequest!: FormGroup;

    constructor(private http: HttpClient, private cookieService: CookieService, private fb: FormBuilder, private router: Router, private sanitizer: DomSanitizer, private errorHandler: ErrorHandlerService) {
        this.user.id = this.cookieService.get('UserId');
    }

    ngOnInit(): void {
        this.getUser(this.user.id);
        this.loadProfileData(this.user.id);
        this.passwordChangeRequest = this.fb.group({
            oldPassword: ['', Validators.required],
            newPassword: ['', Validators.required]
        })
    }

    getUser(userId: string) {
        if (userId) {
            this.http.get(`https://localhost:7045/User/getUserCredentials?userId=${userId}`, { withCredentials: true })
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

    loadProfileData(username: string) {
        if (username) {
            this.http.get(`https://localhost:7045/User/GetImage/${username}`, { withCredentials: true, responseType: 'blob' })
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
        this.http.post('https://localhost:7045/User/ChangeAvatar', request, { withCredentials: true})
        .pipe(
            this.errorHandler.handleError401()
        )
        .subscribe((response: any) => {
            if (response && response.status === 'Ok') {
                location.reload();
            }
        }, 
        (error) => {
            if (error.status === 403) {
                this.errorHandler.handleError403(error);
            }
        });
    }

    changePassword(data: ChangePasswordRequest) {
        data.id = this.user.id;

        this.http.patch('https://localhost:7045/User/ChangePassword', data, { withCredentials: true})
        .pipe(
            this.errorHandler.handleError401()
        )
        .subscribe((response: any) => {
            if (response) {
                this.router.navigate(['/']);
            }
        }, 
        (error) => {
            if (error.status === 403) {
                this.errorHandler.handleError403(error);
            }
        });
    }

    changeEmail(data: ChangeEmailRequest) {
        this.http.patch('https://localhost:7045/User/ChangeEmail', data, { withCredentials: true})
        .pipe(
            this.errorHandler.handleError401()
        )
        .subscribe((response: any) => {
            console.log(response);
            if (response) {
                alert("Email change succeeded!");
            }
        }, 
        (error) => {
            if (error.status === 403) {
                this.errorHandler.handleError403(error);
            }
        })
    }
}

