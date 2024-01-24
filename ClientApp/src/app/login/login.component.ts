import { Component } from '@angular/core';
import { LoginRequest } from '../model/LoginRequest';
import { HttpClient } from '@angular/common/http';
import { CookieService } from 'ngx-cookie-service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})

export class LoginComponent {
    constructor(private http: HttpClient, private cookieService: CookieService, private router: Router) { }

    ngOnInit() {
        if (this.isLoggedIn()) {
            this.router.navigate(['/']);
        }
    }

    isLoggedIn() : boolean {
        return this.cookieService.check('Token') && this.cookieService.check('Username');
    }
    
    CreateTask(data: LoginRequest) {
        this.http.post('http://localhost:5000/Auth/Login', data)
        .subscribe((response: any) => {
            console.log(data);
            const expirationTime = new Date();
            expirationTime.setTime(expirationTime.getTime() + (1 * 60 * 60 * 1000));

            this.cookieService.set('Token', response.token, {expires: expirationTime});
            this.cookieService.set('Username', response.username, {expires: expirationTime});
            
            this.router.navigate(['/']);
        }, 
        (error) => {
            if (error.status === 400) {
                alert("Invalid username or password.");
            } else {
                console.error("An error occurred:", error);
            }
        });
    }
}
