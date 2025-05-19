import { Component, OnInit } from '@angular/core';
import { CookieService } from 'ngx-cookie-service';
import { Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { AuthService } from '../services/auth-service/auth.service';
import { AuthResponse } from '../model/responses/auth-responses.model';
import { LoginAuthTokenRequest, LoginRequest } from '../model/auth-requests/auth-requests';

@Component({
    selector: 'app-login',
    templateUrl: './login.component.html',
    styleUrl: '../../styles.css',
    providers: [ MessageService ]
})

export class LoginComponent implements OnInit {
    loadGoogleSigninLibrary: any;
    
    constructor(
        private cookieService: CookieService,
        private router: Router,
        private messageService: MessageService,
        private authService: AuthService
    ) { }

    isLoading: boolean = false;
    loginStarted: boolean = false;
    loginRequest: LoginRequest = { userName: "", password: "", rememberMe: false };

    ngOnInit(): void {        
        if (this.isLoggedIn()) {
            this.router.navigate(['/']);
        }

        setTimeout(() => {
            const urlParams = new URLSearchParams(window.location.search);
            const loginSuccessParam = urlParams.get('registrationSuccess');

            if (loginSuccessParam === 'true') {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: 'Successful registration.',
                    styleClass: 'ui-toast-message-success' });
            } else if (loginSuccessParam === 'false') {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'Unsuccessful registration, please try again later.'
                });
            }

            const newUrl = window.location.pathname + window.location.search.replace('?registrationSuccess=true', '').replace('?registrationSuccess=false', '');
            history.replaceState({}, document.title, newUrl);
        }, 0);
    }

    isLoggedIn() : boolean {
        return this.cookieService.check('UserId');
    }

    loginStartedMethod() : boolean {
        return this.loginStarted;
    }
    
    createTask(form: LoginRequest) {
        this.isLoading = true;
        this.authService.sendLoginToken(form)
        .subscribe((response: AuthResponse<string>) => {
            if (response.isSuccess) {
                this.loginRequest.userName = form.userName;
                this.loginRequest.rememberMe = form.rememberMe;
                this.loginStarted = true;
                this.isLoading = false;
            }
        }, 
        (error) => {
            console.log(error);
            if (error.status === 400 || error.status === 404) {
                this.isLoading = false;

                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: `${error.error}`
                });

            } else {
                this.isLoading = false;

                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'Something unusual happened. Try again later.'
                });
            }
        });
    }

    sendLoginToken(token: string) {
        this.isLoading = true;
        const expirationDate = new Date();
        expirationDate.setFullYear(expirationDate.getFullYear() + 10);
        const request: LoginAuthTokenRequest = {
            userName: this.loginRequest.userName,
            rememberMe : this.loginRequest.rememberMe,
            token: token
    };

        this.authService.login(request)
        .subscribe((response: AuthResponse<string>) => {
            if (response.isSuccess) {
                this.loginStarted = false;
                this.isLoading = false;
                this.router.navigate(['/'], { queryParams: { loginSuccess: 'true' } });
            }
        }, 
        (error) => {
            this.isLoading = false;
            if (error.status === 400) {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: `${error.error.message}`
                });
            } else {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'Something unusual happened. Try again later.'
                });
            }
        });
    }

    cancelLogin() {
        this.loginStarted = false;
    }
}
