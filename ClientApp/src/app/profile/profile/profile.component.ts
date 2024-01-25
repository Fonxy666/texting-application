import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CookieService } from 'ngx-cookie-service';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.css'
})

export class ProfileComponent implements OnInit {
    constructor(private http: HttpClient, private cookieService: CookieService) {
        this.user = this.cookieService.get('Username') ? 
            this.cookieService.get('Username')! : sessionStorage.getItem('Username')!;
    }

    profilePic: string = "";

    ngOnInit(): void {
        this.getUser(this.user);
    }

    user: string;

    getUser(username: string) {
        if (username) {
            const params = { username };
            this.http.get('http://localhost:5000/User/getUser', { params })
                .subscribe((response: any) => {
                    if (response) {
                        console.log(response.imageUrl);
                    }
                });
        } else {
            console.error('Username parameter is null or undefined.');
        }
    }
}
