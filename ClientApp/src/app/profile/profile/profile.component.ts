import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CookieService } from 'ngx-cookie-service';
import { FormGroup, FormBuilder, Validators } from '@angular/forms';
import { ChangePasswordRequest } from '../../model/ChangePasswordRequest';
import { Router } from '@angular/router';
import { ChangeEmailRequest } from '../../model/ChangeEmailRequest';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.css'
})

export class ProfileComponent implements OnInit {
    
    myImage: string = "./assets/images/chat-mountain.jpg";
    user: { name: string, image: string, token: string, email: string, twoFactorEnabled: boolean } = { name: '', image: '', token: '', email: '', twoFactorEnabled: false };
    passwordChangeRequest!: FormGroup;

    constructor(private http: HttpClient, private cookieService: CookieService, private fb: FormBuilder, private router: Router) {
        this.user.name = this.cookieService.get('Username') ? 
            this.cookieService.get('Username')! : sessionStorage.getItem('Username')!;
        this.user.token = this.cookieService.get('Token') ? 
            this.cookieService.get('Token')! : sessionStorage.getItem('Token')!;
    }

    ngOnInit(): void {
        const username = this.cookieService.get('Username') || sessionStorage.getItem('Username');
        this.getUser(username!);
        this.loadProfileData(username!);
        this.passwordChangeRequest = this.fb.group({
            oldPassword: ['', Validators.required],
            newPassword: ['', Validators.required]
        })
    }

    OnPasswordChangeFormSubmit() {
        const changeRequest = new ChangePasswordRequest(
            this.user.email,
            this.passwordChangeRequest.get('password')?.value,
            this.passwordChangeRequest.get('rememberme')?.value
        )
    }

    getUser(username: string) {

        if (username) {
            const params = { username };
            this.http.get('http://localhost:5000/User/getUserCredentials', { 
                params,
                headers: {
                    "Content-Type": "application/json",
                    "Authorization": `Bearer ${this.user.token}`
                }
            })
                .subscribe((response: any) => {
                    if (response) {
                        console.log(response);
                        this.user.email = response.email;
                        this.user.twoFactorEnabled = response.twoFactorEnabled;
                    }
                });
        } else {
            console.error('Username parameter is null or undefined.');
        }
    }

    loadProfileData(username: string) {
        if (username) {
            this.http.get(`http://localhost:5000/User/GetImage/${username}`, { responseType: 'blob' })
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
                        console.log(error);
                        console.log("There is no Avatar for this user.");
                        this.user.image = "https://ptetutorials.com/images/user-profile.png";
                    }
                );
        }
    }

    changePassword(data: ChangePasswordRequest) {
        this.http.patch('http://localhost:5000/User/ChangePassword', data, {
            headers: {
                "Content-Type": "application/json",
                "Authorization": `Bearer ${this.user.token}`
            }
        })
        .subscribe((response: any) => {
            if (response) {
                this.router.navigate(['/']);
            }
        })
    }

    changeEmail(data: ChangeEmailRequest) {
        console.log(data);
        this.http.patch('http://localhost:5000/User/ChangeEmail', data, {
            headers: {
                "Content-Type": "application/json",
                "Authorization": `Bearer ${this.user.token}`
            }
        })
        .subscribe((response: any) => {
            console.log(response);
            if (response) {
                alert("Email change succeeded!");
            }
        })
    }
}

