import { Component } from '@angular/core';
import { CookieService } from 'ngx-cookie-service';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';

@Component({
  selector: 'app-settings',
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.css'
})
export class SettingsComponent {

    constructor(private cookieService : CookieService, private http: HttpClient) {}
    myImage: string = "./assets/images/chat-mountain.jpg";
    animate: boolean = (this.cookieService.get('Animation') === 'True');
    anonymous: boolean = (this.cookieService.get('Anonymous') === 'True');

    handleAnimateCheck() {
        const headers = new HttpHeaders({
            'Content-Type': 'application/json'
        });
        const params = new HttpParams().set('request', 'Animation');
        this.http.post('https://localhost:7045/Cookie/ChangeCookies', null, { headers: headers, params: params, responseType: 'text', withCredentials: true })
        .subscribe((response: any) => {
            if (response) {
                this.animate = !this.animate;
                if (this.animate) {
                    alert(["All animations enabled in the webpage."]);
                } else {
                    alert(["All animations disabled in the webpage."]);
                }
            }
        });
    }

    handleAnonymus() {
        const headers = new HttpHeaders({
            'Content-Type': 'application/json'
        });
        const params = new HttpParams().set('request', 'Anonymous');
        this.http.post('https://localhost:7045/Cookie/ChangeCookies', null, { headers: headers, params: params, responseType: 'text', withCredentials: true })
        .subscribe((response: any) => {
            if (response) {
                this.anonymous = !this.anonymous;
                if (this.anonymous) {
                    alert(["Now other users cannot see your username/e-mail."]);
                } else {
                    alert(["Now other users can see your username/e-mail."]);
                }
            }
        });
    }
}
