import { Component } from '@angular/core';
import { CookieService } from 'ngx-cookie-service';

@Component({
  selector: 'app-nav',
  templateUrl: './nav.component.html',
  styleUrls: ['./nav.component.css']
})

export class NavComponent {
    constructor(private cookieService : CookieService) {}

    isLoggedIn() : boolean {
        return this.cookieService.check('Token');
    }

    logout() {
        this.cookieService.delete('Token');
    }
}
