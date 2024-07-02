import { Component, OnInit } from '@angular/core';
import { CookieService } from 'ngx-cookie-service';
import { MessageService } from 'primeng/api';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['../../styles.css', './home.component.css'],
  providers: [ MessageService ]
})

export class HomeComponent implements OnInit {
    constructor(
        private cookieService: CookieService,
        private messageService: MessageService
    ) { }

    starsImage: string = "./assets/images/4-out-of-5-stars.webp"
    animation: boolean = true;
    isLoading: boolean = false;

    ngOnInit() {
        this.animation = this.cookieService.get("Animation") != "False";

        setTimeout(() => {
            const urlParams = new URLSearchParams(window.location.search);
            const loginSuccessParam = urlParams.get('loginSuccess');
            const logoutParam = urlParams.get('logout');

            if (logoutParam == 'true') {
                this.messageService.add({ severity: 'info', summary: 'Info', detail: 'You logged out. Goodbye, hopefully we will meet later on ! :)', styleClass: 'ui-toast-message-info' });
            }

            if (loginSuccessParam == 'true') {
                this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Successful login.', styleClass: 'ui-toast-message-success' });
            } else if (loginSuccessParam === 'false') {
                this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Unsuccessful login, please try again later.' });
            }

            const newUrl = window.location.pathname + window.location.search.replace('?loginSuccess=true', '').replace('?loginSuccess=false', '').replace('?logout=true', '');
            history.replaceState({}, document.title, newUrl);
        }, 0);
    }
}
