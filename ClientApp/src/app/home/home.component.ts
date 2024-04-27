import { Component, OnInit } from '@angular/core';
import { CookieService } from 'ngx-cookie-service';
import { MessageService } from 'primeng/api';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrl: '../../styles.css',
  providers: [ MessageService ]
})

export class HomeComponent implements OnInit {
    constructor(private cookieService: CookieService, private messageService: MessageService) { }

    myImage: string = "./assets/images/backgroundpng.png";
    isSunActive: boolean = true;
    isMoonActive: boolean = false;
    displayHomeText: string = "";
    animation: boolean = true;
    isLoading: boolean = false;

    ngOnInit() {
        this.animation = this.cookieService.get("Animation") != "False";
        
        this.toggleImageClasses();

        setInterval(() => {
            this.toggleImageClasses();
        }, 10000);

        setTimeout(() => {
            const urlParams = new URLSearchParams(window.location.search);
            const loginSuccessParam = urlParams.get('loginSuccess');

            if (loginSuccessParam === 'true') {
                this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Successful login.', styleClass: 'ui-toast-message-success' });
            } else if (loginSuccessParam === 'false') {
                this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Unsuccessful login, please try again later.' });
            }

            const newUrl = window.location.pathname + window.location.search.replace('?loginSuccess=true', '').replace('?loginSuccess=false', '');
            history.replaceState({}, document.title, newUrl);
        }, 0);
    }


    toggleImageClasses() {
        this.isSunActive = !this.isSunActive;
    }
}
