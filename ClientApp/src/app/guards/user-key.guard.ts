import { Injectable } from '@angular/core';
import { CanActivate } from '@angular/router';
import { CookieService } from 'ngx-cookie-service';
import { IndexedDBService } from '../services/db-service/indexed-dbservice.service';
import { TokenProvideComponent } from '../token-provide/token-provide.component';
import { MatDialog } from '@angular/material/dialog';

@Injectable({
    providedIn: 'root'
  })

export class UserKeyGuard implements CanActivate {
    
    userId: string = this.cookieService.get("UserId");

    constructor(
        private dbService: IndexedDBService,
        private cookieService: CookieService,
        private dialog: MatDialog
    ) { }
    
    async canActivate(): Promise<boolean> {
        try {
            const userKey = await this.dbService.getEncryptionKey(this.userId);

            if (userKey) {
                console.log("UserKeyGuard: User key found");
                return true;
            } else {
                console.log("UserKeyGuard: User key not found");
                this.openTokenProvideModal();
                return false;
            }

        } catch (error) {
            console.error("UserKeyGuard: Error fetching user key", error);
            this.openTokenProvideModal();
            return false;
        }
    }

    private openTokenProvideModal(): void {
        const dialogRef = this.dialog.open(TokenProvideComponent, {
            data: {
                pageName: 'Provide Token',
                labelName: 'Enter Token:',
                inputPlaceholder: 'Enter your token',
                buttonContext: 'Submit'
            },
            disableClose: true
        });

        dialogRef.afterClosed().subscribe(result => {
            // Handle close events if required
        });
    }
}