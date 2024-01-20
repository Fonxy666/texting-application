import { Component } from '@angular/core';
import { CookieService } from 'ngx-cookie-service';

@Component({
  selector: 'app-nav-bar',
  templateUrl: './nav-bar.component.html',
  styleUrl: './nav-bar.component.css'
})
export class NavBarComponent {
    constructor(private cookieService : CookieService) {}

    isLoggedIn() : boolean {
        return this.cookieService.check('Token');
    }

    logout() {
        this.cookieService.delete('Token');
    }
}
