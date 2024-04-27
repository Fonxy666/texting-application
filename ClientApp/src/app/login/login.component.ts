import { Component, OnInit } from '@angular/core';
import { LoginRequest } from '../model/LoginRequest';
import { HttpClient } from '@angular/common/http';
import { CookieService } from 'ngx-cookie-service';
import { Router } from '@angular/router';
import { LoginAuthTokenRequest } from '../model/LoginAuthTokenRequest';
import { MessageService } from 'primeng/api';
import { NotificationService } from '../services/toast-message.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrl: '../../styles.css',
  providers: [ MessageService ]
})

export class LoginComponent implements OnInit {
    loadGoogleSigninLibrary: any;
    constructor(private http: HttpClient, private cookieService: CookieService, private router: Router, private messageService: MessageService, private notificationService: NotificationService) { }

    isLoading: boolean = false;
    loginStarted: boolean = false;
    loginRequest: LoginRequest = new LoginRequest("", "", false);

    ngOnInit(): void {        
        if (this.isLoggedIn()) {
            this.router.navigate(['/']);
        }

        this.notificationService.message$.subscribe(message => {
            this.messageService.add(message);
        });
    }

    isLoggedIn() : boolean {
        return this.cookieService.check('UserId');
    }

    loginStartedMethod() : boolean {
        return this.loginStarted;
    }
    
    createTask(data: LoginRequest) {
        this.isLoading = true;
        this.http.post('https://localhost:7045/Auth/SendLoginToken', data, { withCredentials: true })
        .subscribe((response: any) => {
            if (response.success) {
                this.loginRequest.username = data.username;
                this.loginRequest.rememberme = data.rememberme;
                this.loginStarted = true;
                this.isLoading = false;
            }
        }, 
        (error) => {
            if (error.status === 404) {
                if (!isNaN(error.error)) {
                    alert(`Invalid username or password, you have ${5-error.error} tries.`);
                } else {
                    alert(error.error);
                }
            } else {
                console.error("An error occurred:", error);
            }
        });
    }

    sendLoginToken(token: string) {
        this.isLoading = true;
        const expirationDate = new Date();
        expirationDate.setFullYear(expirationDate.getFullYear() + 10);
        const request = new LoginAuthTokenRequest(this.loginRequest.username, this.loginRequest.password, this.loginRequest.rememberme, token);

        this.http.post('https://localhost:7045/Auth/Login', request, { withCredentials: true })
        .subscribe((response: any) => {
            if (response.success) {
                this.loginStarted = false;
                this.isLoading = false;
                this.notificationService.setMessage({ severity: 'success', summary: 'Success', detail: 'Successfull login.' });
                console.log("settelve");
                this.router.navigate(['/']);
            }
        }, 
        (error) => {
            alert(["Wrong token!"]);
            console.error("An error occurred:", error);
        });
    }

    cancelLogin() {
        this.loginStarted = false;
    }
}
