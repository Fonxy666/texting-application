import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { CookieService } from 'ngx-cookie-service';

@Component({
  selector: 'app-nav-bar',
  templateUrl: './nav-bar.component.html',
  styleUrl: './nav-bar.component.css'
})

export class NavBarComponent implements OnInit {
    constructor(private cookieService : CookieService, private router: Router, private http: HttpClient) {}

    ngOnInit(): void {
        this.loadProfileData();
    }

    profilePic: string = "";

    isLoggedIn(): boolean {
        return !!((this.cookieService.check('Token') && this.cookieService.check('Username')) ||
                   (sessionStorage.getItem('Token') && sessionStorage.getItem('Username')));
    }

    loadProfileData() {
        const username = this.cookieService.get('Username') || sessionStorage.getItem('Username');
        
        if (username) {
            this.http.get(`http://localhost:5000/User/GetImage/${username}`, { responseType: 'blob' })
                .subscribe(
                    (response: any) => {
                        if (response instanceof Blob) {
                                                        const reader = new FileReader();
                            reader.onloadend = () => {
                                this.profilePic = reader.result as string;
                            };
                            reader.readAsDataURL(response);
                        }
                    },
                    (error) => {
                        console.log(error);
                        console.log("There is no Avatar for this user.");
                        this.profilePic = "https://ptetutorials.com/images/user-profile.png";
                    }
                );
        }
    }

    logout() {
        if (this.cookieService.check('Token') && this.cookieService.check('Username')) {
            this.cookieService.delete('Token');
            this.cookieService.delete('Username');
        } else {
            sessionStorage.removeItem('Token');
            sessionStorage.removeItem('Username');
        }
        this.router.navigate(['/']);
    }
}
