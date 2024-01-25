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
        if (this.cookieService.check('Token') && this.cookieService.check('Username') || sessionStorage.getItem('Token') && sessionStorage.getItem('Username')) {
            return true;
        } else {
            return false;
        }
    }

    logout() {
        if (this.cookieService.check('Token') && this.cookieService.check('Username')) {
            this.cookieService.delete('Token');
            this.cookieService.delete('Username');
        } else {
            sessionStorage.removeItem('Token');
            sessionStorage.removeItem('Username');
        }
        this.router.navigate(['/']);
    }
}
