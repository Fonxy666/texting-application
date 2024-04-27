import { Component, OnInit } from '@angular/core';
import { CookieService } from 'ngx-cookie-service';
import { MessageService } from 'primeng/api';
import { NotificationService } from '../services/toast-message.service';
import { ToastMessage } from '../model/ToastMessage';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrl: '../../styles.css',
  providers: [ MessageService ]
})

export class HomeComponent implements OnInit {
    constructor(private cookieService: CookieService, private messageService: MessageService, private notificationService: NotificationService) { }

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
                this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Successfull login.' });
            } else if (loginSuccessParam === 'false') {
                this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Unsuccessful login, please try again later.' });
            }
        }, 0);


        setTimeout(() => {
            this.notificationService.message$.subscribe(message => {
                this.show(message);
            });
        }, 0);
    }
    
    show(message: ToastMessage) {
        this.messageService.add({ severity: message['Severity'], summary: message['Summary'], detail: message['Detail'] });
    }


    toggleImageClasses() {
        this.isSunActive = !this.isSunActive;
    }
}
