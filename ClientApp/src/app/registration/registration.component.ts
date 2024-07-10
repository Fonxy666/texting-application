import { Component } from '@angular/core';
import { RegistrationRequest } from '../model/auth-requests/RegistrationRequest';
import { Router } from '@angular/router';
import { TokenValidatorRequest } from '../model/auth-requests/TokenValidatorRequest';
import { MessageService } from 'primeng/api';
import { AuthService } from '../services/auth-service/auth.service';

@Component({
  selector: 'app-registration',
  templateUrl: './registration.component.html',
  providers: [ MessageService ],
})

export class RegistrationComponent {
    constructor(
        private router: Router,
        private messageService: MessageService,
        private authService: AuthService
    ) { }

    isLoading: boolean = false;
    user: any;
    showVerifyPage: boolean = false;

    getVerifyTokenAndGoToVerifyPage(data: RegistrationRequest) {
        this.sendVerifyEmail(data);
    }

    sendVerifyEmail(data: any) {
        this.isLoading = true;
        this.authService.sendVerifyEmail({ Email: data.email })
        .subscribe((response: any) => {
            if (response) {
                this.user = data;
                this.isLoading = false;
                this.showVerifyPage = true;
            }
        },
        (error) => {
            this.isLoading = false;
            console.error("An error occurred:", error);
        });
    }

    getVerifyTokenAndSendRegistration(verifyCode: String) {
        this.isLoading = true;
        const request = new TokenValidatorRequest(this.user.email, verifyCode.toString());
        this.authService.examineVerifyToken(request)
        .subscribe((response: any) => {
            if (response) {
                this.sendRegistration();
                this.isLoading = false;
                this.router.navigate(['login'], { queryParams: { registrationSuccess: 'true' } });
            }
        },
        (error) => {
            this.isLoading = false;
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: 'Wrong token.'
            });
            console.error("An error occurred:", error);
        });
    }

    sendRegistration() {
        this.isLoading = true;
        this.authService.registration(this.user)
        .subscribe(response => {
            console.log(response);
        },
        (error) => {
            this.isLoading = false;
            console.error("An error occurred:", error);
        });
    }

    cancelLogin() {
        this.router.navigate(['/']);
    }
}
