import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { AuthService } from '../services/auth-service/auth.service';
import { AuthResponse } from '../model/responses/auth-responses.model';
import { RegistrationRequest, TokenValidatorRequest } from '../model/auth-requests/auth-requests';

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
    user: RegistrationRequest | undefined;
    showVerifyPage: boolean = false;

    getVerifyTokenAndGoToVerifyPage(data: RegistrationRequest) {
        this.sendVerifyEmail(data);
    }

    sendVerifyEmail(data: RegistrationRequest) {
        this.isLoading = true;
        this.authService.sendVerifyEmail({ email: data.email, userName: data.userName })
        .subscribe((response: AuthResponse<string>) => {
            if (response.isSuccess) {
                this.user = data;
                this.isLoading = false;
                this.showVerifyPage = true;
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: `${response.data}`,
                    styleClass: 'ui-toast-message-success'
                });
            } else if (!response.isSuccess) {
                this.isLoading = false;
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: `${response.message}`
                });
            }
        },
        (error) => {
            this.isLoading = false;
            console.error("An error occurred:", error);
        });
    }

    getVerifyTokenAndSendRegistration(verifyCode: String) {
        this.isLoading = true;
        const request: TokenValidatorRequest = { email: this.user!.email, verifyCode: verifyCode.toString() };
        this.authService.examineVerifyToken(request)
        .subscribe((response: AuthResponse<string>) => {
            if (response.isSuccess) {
                this.sendRegistration();
                this.isLoading = false;
                this.router.navigate(['login'], { queryParams: { registrationSuccess: 'true' } });
            } else if (!response.isSuccess) {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: `${response.message}`
                });
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
        this.authService.registration(this.user!)
        .subscribe((response: AuthResponse<string>) => {
            if (response.isSuccess) {
                this.router.navigate(['login'], { queryParams: { registrationSuccess: 'true' } });
            } else {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: `${response.message}`
                });
            }
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
