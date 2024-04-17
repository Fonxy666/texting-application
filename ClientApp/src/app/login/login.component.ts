import { Component, OnInit, NgZone } from '@angular/core';
import { LoginRequest } from '../model/LoginRequest';
import { HttpClient } from '@angular/common/http';
import { CookieService } from 'ngx-cookie-service';
import { Router } from '@angular/router';
import { LoginAuthTokenRequest } from '../model/LoginAuthTokenRequest';
import { CredentialResponse, PromptMomentNotification } from 'google-one-tap';
import { AuthService } from '../services/auth/auth.service';


@Component({
  selector: 'app-login',
  templateUrl: './login.component.html'
})

export class LoginComponent implements OnInit {
    loadGoogleSigninLibrary: any;
    constructor(private http: HttpClient, private cookieService: CookieService, private router: Router, private _ngZone: NgZone, private service: AuthService) { }

    loginStarted: boolean = false;
    loginRequest: LoginRequest = new LoginRequest("", "", false);

    ngOnInit(): void {
        // @ts-ignore
        window.onGoogleLibraryLoad = () => {
            //@ts-ignore
            google.accounts.id.initialize({
                client_id: '',
                callback: this.handleCredentialResponse.bind(this),
                auto_select: false,
                cancel_on_tap_outside: true
            });
            // @ts-ignore
            google.accounts.id.renderButton(
                // @ts-ignore
                document.getElementById("buttonDiv"),
                {theme: "outline", size: "large", width: "100%"}
            );

            // @ts-ignore
            google.accounts.id.prompt((notification: PromptMomentNotification) => {});
        }
        
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
        const expirationDate = new Date();
        expirationDate.setFullYear(expirationDate.getFullYear() + 10);
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

    async handleCredentialResponse(response: CredentialResponse) {
        await this.service.loginWithGoogle(response.credential).subscribe(
            (x: any) => {
                localStorage.setItem("token", x.token);
                this._ngZone.run(() => {
                    this.router.navigate(['/']);
                })
            }
        )
    }
}
