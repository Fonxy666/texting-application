import { Component, OnInit } from '@angular/core';
import { RegistrationRequest } from '../model/RegistrationRequest';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Router } from '@angular/router';
import { TokenValidatorRequest } from '../model/TokenValidatorRequest';

@Component({
  selector: 'app-registration',
  templateUrl: './registration.component.html',
  styleUrl: './registration.component.css'
})

export class RegistrationComponent implements OnInit  {
    constructor(private http: HttpClient, private router: Router) { }

    user: any;
    showVerifyPage: boolean = false;

    ngOnInit(): void {
        
    }

    GetVerifyTokenAndGoToVerifyPage(data: RegistrationRequest) {
        this.SendVerifyEmail(data);
    }

    SendVerifyEmail(data: any) {
        const requestData = { Email: data.email };
        const headers = new HttpHeaders({
            'Content-Type': 'application/json'
        });

        this.http.post('https://localhost:7045/Auth/GetEmailVerificationToken', requestData, { headers: headers, responseType: 'text' })
            .subscribe((response: any) => {
                if (response) {
                    this.user = data;
                    this.showVerifyPage = true;
                }
            });
    }

    GetVerifyTokenAndSendRegistration(verifyCode: String) {
        const request = new TokenValidatorRequest(this.user.email, verifyCode.toString());
        const headers = new HttpHeaders({
            'Content-Type': 'application/json'
        });
        this.http.post('https://localhost:7045/Auth/ExamineVerifyToken', request, { headers: headers, responseType: 'text' })
            .subscribe((response: any) => {
                if (response) {
                    this.SendRegistration();
                    this.router.navigate(['login']);
                }
            });
    }

    SendRegistration() {
        this.http.post('https://localhost:7045/Auth/Register', this.user)
            .subscribe((response) => {
                if (response) {
                    alert("Succesfull registration!");  
                }
            });
    }
}
