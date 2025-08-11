import { Component } from '@angular/core';
import { CookieService } from 'ngx-cookie-service';
import { MessageService } from 'primeng/api';
import { CookiesService } from '../../services/cookie-service/cookies.service';

@Component({
    selector: 'app-settings',
    templateUrl: './settings.component.html',
    styleUrl: './settings.component.css',
    providers: [ MessageService ]
})
export class SettingsComponent {

    constructor(
        private cookieService : CookieService,
        private messageService: MessageService,
        private cookiesService: CookiesService
    ) {}

    animate: boolean = (this.cookieService.get('Animation') === 'True');
    anonymous: boolean = (this.cookieService.get('Anonymous') === 'True');

    handleAnimateCheck() {
        this.cookiesService.changeCookies('Animation')
        .subscribe((response: any) => {
            if (response) {
                this.animate = !this.animate;
                if (this.animate) {
                    this.messageService.add({
                        severity: 'info',
                        summary: 'Info',
                        detail: 'All animations enabled in the webpage.',
                        styleClass: 'ui-toast-message-info'
                    });
                } else {
                    this.messageService.add({
                        severity: 'info',
                        summary: 'Info',
                        detail: 'All animations disabled in the webpage.',
                        styleClass: 'ui-toast-message-info'
                    });
                }
            }
        });
    }

    handleAnonymus() {
        this.cookiesService.changeCookies('Anonymous')
        .subscribe((response: any) => {
            if (response) {
                this.anonymous = !this.anonymous;
                if (this.anonymous) {
                    this.messageService.add({
                        severity: 'info',
                        summary: 'Info',
                        detail: 'Now other users cannot see your username/e-mail.',
                        styleClass: 'ui-toast-message-info'
                    });
                } else {
                    this.messageService.add({
                        severity: 'info',
                        summary: 'Info',
                        detail: 'Now other users can see your username/e-mail.',
                        styleClass: 'ui-toast-message-info' });
                }
            }
        });
    }
}
