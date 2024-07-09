import { Component, OnInit } from '@angular/core';
import { LoginRequest } from '../model/auth-requests/LoginRequest';
import { CookieService } from 'ngx-cookie-service';
import { Router } from '@angular/router';
import { LoginAuthTokenRequest } from '../model/auth-requests/LoginAuthTokenRequest';
import { MessageService } from 'primeng/api';
import { AuthService } from '../services/auth-service/auth.service';

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
    loginRequest: LoginRequest = new LoginRequest("", "", false);

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
        .subscribe((response: any) => {
            if (response.success) {
                this.loginRequest.username = form.username;
                this.loginRequest.rememberme = form.rememberme;
                this.loginStarted = true;
                this.isLoading = false;
            }
        }, 
        (error) => {
            if (error.status === 400) {
                if (!isNaN(error.error)) {
                    console.log(error);
                    if (error.error == 4) {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: `Only 1 more try.`
                        });
                    } else {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: `Invalid username or password, you have ${5-error.error} tries.`
                        });
                    }

                    this.isLoading = false;
                } else {
                    this.isLoading = false;
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Error',
                        detail: `${error.error.split(".")[0]}. ${error.error.split(".")[1]}`
                    });
                }
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
        const request = new LoginAuthTokenRequest(
            this.loginRequest.username,
            this.loginRequest.password,
            this.loginRequest.rememberme, token
        );

        this.authService.login(request)
        .subscribe((response: any) => {
            if (response.success) {
                console.log(response);
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
                    detail: 'Wrong token.'
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
