import { Component } from '@angular/core';
import { CookieService } from 'ngx-cookie-service';

@Component({
  selector: 'app-settings',
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.css'
})
export class SettingsComponent {

    constructor(private cookieService : CookieService) {}
    myImage: string = "./assets/images/chat-mountain.jpg";
    animate: boolean = (this.cookieService.get('Animation') === 'true');
    anonymous: boolean = (this.cookieService.get('Anonymous') === 'true');

    handleAnimateCheck() {
        console.log(this.animate);
    }

    handleAnonymus() {
        console.log(this.anonymous);
    }
}
