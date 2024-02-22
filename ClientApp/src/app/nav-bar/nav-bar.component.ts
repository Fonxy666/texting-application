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
        return this.cookieService.check('UserId');
    }

    loadProfileData() {
        const userId = this.cookieService.get('UserId');
        
        if (userId) {
            this.http.get(`https://localhost:7045/User/GetImage/${userId}`, { withCredentials: true, responseType: 'blob' })
                .subscribe(
                    (response: Blob) => {
                        const reader = new FileReader();
                        reader.onloadend = () => {
                            this.profilePic = reader.result as string;
                        };
                        reader.readAsDataURL(response);
                    },
                    (error) => {
                        console.error(error);
                        console.log("There is no Avatar for this user.");
                        this.profilePic = "https://ptetutorials.com/images/user-profile.png";
                    }
                );
        }
    }

    logout() {
        this.http.post('https://localhost:7045/Auth/Logout', {}, { withCredentials: true })
        .subscribe((response: any) => {
            if (response.success) {
                this.router.navigate(['/']);
            }
        }, 
        (error) => {
            console.error("An error occurred:", error);
        });
    }
}
