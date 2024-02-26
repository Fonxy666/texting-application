import { Component } from '@angular/core';
import { LoginRequest } from '../model/LoginRequest';
import { HttpClient } from '@angular/common/http';
import { CookieService } from 'ngx-cookie-service';
import { Router } from '@angular/router';
import { LoginAuthTokenRequest } from '../model/LoginAuthTokenRequest';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})

export class LoginComponent {
    constructor(private http: HttpClient, private cookieService: CookieService, private router: Router) { }

    loginStarted: boolean = false;
    loginRequest: LoginRequest = new LoginRequest("", "", false);

    ngOnInit() {
        if (this.isLoggedIn()) {
            this.router.navigate(['/']);
        }
    }

    isLoggedIn() : boolean {
        return this.cookieService.check('UserId');
    }

    loginStartedMethod() : boolean {
        return this.loginStarted;
    }
    
    CreateTask(data: LoginRequest) {
        this.http.post('https://localhost:7045/Auth/SendLoginToken', data, { withCredentials: true })
        .subscribe((response: any) => {
            if (response.success) {
                this.loginRequest.username = data.username;
                this.loginRequest.rememberme = data.rememberme;
                this.loginStarted = true;
            }
        }, 
        (error) => {
            if (error.status === 404) {
                if (!isNaN(error.error)) {
                    alert(`Invalid username or password, you have ${5-error.error} tries.`);
                } else {
                    var errorMessage = error.error.split(".")[0] + "." + error.error.split(".")[1];
                    alert(errorMessage);
                }
            } else {
                console.error("An error occurred:", error);
            }
        });
    }

    SendLoginToken(token: string) {
        const request = new LoginAuthTokenRequest(this.loginRequest.username, this.loginRequest.password, this.loginRequest.rememberme, token);
        this.http.post('https://localhost:7045/Auth/Login', request, { withCredentials: true })
        .subscribe((response: any) => {
            if (response.success) {
                this.loginStarted = false;
                this.loginRequest = new LoginRequest("", "", false);
                this.router.navigate(['/']);
            }
        }, 
        (error) => {
            alert(["Wrong token!"]);
            console.error("An error occurred:", error);
        });
    }
}
