import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
import { CookieService } from 'ngx-cookie-service';

@Injectable({
  providedIn: 'root'
})
export class AuthGuard implements CanActivate {
    constructor(private cookieService: CookieService, private router: Router) {}

    isLoggedIn: boolean = this.cookieService.get("UserId") !== "";

    canActivate(): boolean {
        if (this.isLoggedIn) {
            return true;
        } else {
            this.router.navigate(['/']);
            return false;
        }
    }
}