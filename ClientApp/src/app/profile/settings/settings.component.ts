import { Component } from '@angular/core';
import { CookieService } from 'ngx-cookie-service';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { MessageService } from 'primeng/api';

@Component({
    selector: 'app-settings',
    templateUrl: './settings.component.html',
    styleUrl: './settings.component.css',
    providers: [ MessageService ]
})
export class SettingsComponent {

    constructor(private cookieService : CookieService, private http: HttpClient, private messageService: MessageService) {}
    myImage: string = "./assets/images/chat-mountain.jpg";
    animate: boolean = (this.cookieService.get('Animation') === 'True');
    anonymous: boolean = (this.cookieService.get('Anonymous') === 'True');

    handleAnimateCheck() {
        const headers = new HttpHeaders({
            'Content-Type': 'application/json'
        });
        const params = new HttpParams().set('request', 'Animation');
        this.http.post(`/api/v1/Cookie/ChangeCookies`, null, { headers: headers, params: params, responseType: 'text', withCredentials: true })
        .subscribe((response: any) => {
            if (response) {
                this.animate = !this.animate;
                if (this.animate) {
                    this.messageService.add({ severity: 'info', summary: 'Info', detail: 'All animations enabled in the webpage.', styleClass: 'ui-toast-message-info' });
                } else {
                    this.messageService.add({ severity: 'info', summary: 'Info', detail: 'All animations disabled in the webpage.', styleClass: 'ui-toast-message-info' });
                }
            }
        });
    }

    handleAnonymus() {
        const headers = new HttpHeaders({
            'Content-Type': 'application/json'
        });
        const params = new HttpParams().set('request', 'Anonymous');
        this.http.post(`/api/v1/Cookie/ChangeCookies`, null, { headers: headers, params: params, responseType: 'text', withCredentials: true })
        .subscribe((response: any) => {
            if (response) {
                this.anonymous = !this.anonymous;
                if (this.anonymous) {
                    this.messageService.add({ severity: 'info', summary: 'Info', detail: 'Now other users cannot see your username/e-mail.', styleClass: 'ui-toast-message-info' });
                } else {
                    this.messageService.add({ severity: 'info', summary: 'Info', detail: 'Now other users can see your username/e-mail.', styleClass: 'ui-toast-message-info' });
                }
            }
        });
    }
}
