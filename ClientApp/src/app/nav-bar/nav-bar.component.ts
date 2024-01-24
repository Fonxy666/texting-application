import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { CookieService } from 'ngx-cookie-service';

@Component({
  selector: 'app-nav-bar',
  templateUrl: './nav-bar.component.html',
  styleUrl: './nav-bar.component.css'
})

export class NavBarComponent {
    constructor(private cookieService : CookieService, private router: Router) {}

    isDropdownOpen = false;

    toggleDropdown() {
        this.isDropdownOpen = !this.isDropdownOpen;
    }

    isLoggedIn() : boolean {
        return this.cookieService.check('Token') && this.cookieService.check('Username');
    }

    logout() {
        this.cookieService.delete('Token');
        this.cookieService.delete('Username');
        this.router.navigate(['/']);
    }
}
