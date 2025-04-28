import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { UserService } from '../services/user-service/user.service';

@Component({
    selector: 'app-forgot-password',
    templateUrl: './forgot-password.component.html',
    styleUrl: '../../styles.css',
    providers: [ MessageService ]
})

export class ForgotPasswordComponent {
    constructor(
        private router: Router,
        private messageService: MessageService,
        private userService: UserService
    ) {}

    isLoading: boolean = false;

    sendPasswordResetEmail(email: string) {
        this.isLoading = true;
        this.userService.forgotPassword(email)
        .subscribe((response) => {
            if (response.isSuccess) {
                this.isLoading = false;
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: 'E-mail successfully sent to the e-mail address.',
                    styleClass: 'ui-toast-message-success'
                });
                setTimeout(() => {
                    window.close();
                }, 5000);
            } else {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: response.message
                });
            }
        },
        (error) => {
            this.isLoading = false;
            console.error("An error occurred:", error);
        });
    }

    cancelLogin() {
        this.router.navigate(['/login']);
    }
}
