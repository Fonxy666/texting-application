import { Component } from '@angular/core';
import { RegistrationRequest } from '../model/RegistrationRequest';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Router } from '@angular/router';
import { TokenValidatorRequest } from '../model/TokenValidatorRequest';
import { MessageService } from 'primeng/api';

@Component({
  selector: 'app-registration',
  templateUrl: './registration.component.html',
  providers: [ MessageService ],
})

export class RegistrationComponent {
    constructor(private http: HttpClient, private router: Router, private messageService: MessageService) { }

    isLoading: boolean = false;
    user: any;
    showVerifyPage: boolean = false;

    getVerifyTokenAndGoToVerifyPage(data: RegistrationRequest) {
        this.sendVerifyEmail(data);
    }

    sendVerifyEmail(data: any) {
        this.isLoading = true;
        const requestData = { Email: data.email };
        const headers = new HttpHeaders({
            'Content-Type': 'application/json'
        });

        this.http.post('https://localhost:7045/Auth/SendEmailVerificationToken', requestData, { headers: headers, responseType: 'text' })
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
        this.http.post('https://localhost:7045/Auth/ExamineVerifyToken', request)
        .subscribe((response: any) => {
            if (response) {
                this.sendRegistration();
                this.isLoading = false;
                this.router.navigate(['login'], { queryParams: { registrationSuccess: 'true' } });
            }
        },
        (error) => {
            this.isLoading = false;
            this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Wrong token.' });
            console.error("An error occurred:", error);
        });
    }

    sendRegistration() {
        this.http.post('https://localhost:7045/Auth/Register', this.user)
        .subscribe(() => {},
        (error) => {
            this.isLoading = false;
            console.error("An error occurred:", error);
        });
    }
}
