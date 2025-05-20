import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { AuthService } from '../services/auth-service/auth.service';
import { RegistrationRequest, TokenValidatorRequest } from '../model/auth-requests/auth-requests';
import { ServerResponse } from '../model/responses/shared-response.model';

@Component({
  selector: 'app-registration',
  templateUrl: './registration.component.html',
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
        .subscribe((response: ServerResponse<string>) => {
            if (response.isSuccess) {
                this.user = data;
                this.isLoading = false;
                this.showVerifyPage = true;
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: `${response.message}`,
                    styleClass: 'ui-toast-message-success'
                });
            } else if (!response.isSuccess) {
                this.isLoading = false;
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: `${response.error!.message}`
                });
            }
        },
        (error) => {
            this.isLoading = false;
            console.error("An error occurred:", error);
        });
    }

    getVerifyTokenAndSendRegistration(verifyCode: String) {
        if (this.isLoading) return;

        this.isLoading = true;
        const request: TokenValidatorRequest = { email: this.user!.email, verifyCode: verifyCode.toString() };
        this.authService.examineVerifyToken(request)
        .subscribe((response: ServerResponse<string>) => {
            if (response.isSuccess) {
                this.sendRegistration();
                this.isLoading = false;
            }
        },
        (error) => {
            console.error("An error occurred:", error);
        });
    }

    sendRegistration() {
        this.isLoading = true;
        this.authService.registration(this.user!)
        .subscribe((response: ServerResponse<string>) => {
            if (response.isSuccess) {
                this.router.navigate(['login'], { queryParams: { registrationSuccess: 'true' } });
            }
        },
        (error) => {
            this.isLoading = false;
            console.log(error);
            this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: `${error.error.message || 'Registration failed.'}`
                });
            setTimeout(() => {
               this.router.navigate(['login'], { queryParams: { registrationSuccess: 'false' } }); 
            }, 3000);
            console.error("An error occurred:", error);
        });
    }

    cancelLogin() {
        this.router.navigate(['/']);
    }
}
