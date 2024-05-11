import { HttpClient } from '@angular/common/http';
import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { MessageService } from 'primeng/api';


@Component({
  selector: 'app-forgot-password',
  templateUrl: './forgot-password.component.html',
  styleUrl: './forgot-password.component.css',
  providers: [ MessageService ]
})
export class ForgotPasswordComponent {
    constructor(private router: Router, private http: HttpClient, private messageService: MessageService) {}

    isLoading: boolean = false;
    tokenSend: boolean = false;

    sendPasswordResetEmail(email: string) {
        this.isLoading = true;
        this.http.get(`/api/v1/User/SendForgotPasswordToken?email=${email}`)
        .subscribe((response: any) => {
            if (response.success) {
                this.isLoading = false;
                this.tokenSend = true;
            }
        },
        (error) => {
            this.isLoading = false;
            this.messageService.add({ severity: 'error', summary: 'Error', detail: 'There is no user with this e-mail.' });
            console.error("An error occurred:", error);
        });
    }

    cancelLogin() {
        this.router.navigate(['/login']);
    }
}
