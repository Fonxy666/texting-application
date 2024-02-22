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
    user: { id: string, name: string, image: string, token: string, email: string, twoFactorEnabled: boolean } = { id: "", name: '', image: '', token: '', email: '', twoFactorEnabled: false };
    passwordChangeRequest!: FormGroup;

    constructor(private http: HttpClient, private cookieService: CookieService, private fb: FormBuilder, private router: Router) {
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

    getUser(username: string) {
        if (username) {
            const params = { username };
            this.http.get('https://localhost:7045/User/getUserCredentials', { withCredentials: true })
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
            this.http.get(`https://localhost:7045/User/GetImage/${username}`, { withCredentials: true, responseType: 'blob' })
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
        data.id = this.user.id;
        console.log(data);

        this.http.patch('https://localhost:7045/User/ChangePassword', data, { withCredentials: true})
        .subscribe((response: any) => {
            console.log(response);
            if (response) {
                this.router.navigate(['/']);
            }
        })
    }

    changeEmail(data: ChangeEmailRequest) {
        this.http.patch('https://localhost:7045/User/ChangeEmail', data, { withCredentials: true})
        .subscribe((response: any) => {
            console.log(response);
            if (response) {
                alert("Email change succeeded!");
            }
        })
    }
}

