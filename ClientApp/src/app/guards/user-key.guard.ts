import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
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
        private dialog: MatDialog,
        private router: Router
    ) { }

    async canActivate(): Promise<boolean> {
        try {
            await this.dbService.dbReady;
            const userKey = await this.dbService.getEncryptionKey(this.userId);

            if (userKey) {
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
                pageName: 'You need to provide the key, what we will use for decrypt the messages, which is for your security.',
                labelName: 'Don`t forget to remember to this key, because you need to give us this key after logins.',
                inputPlaceholder: 'Enter your token',
                buttonContext: 'Submit',
                background: 'false'
            },
            disableClose: true,
            panelClass: 'custom-dialog-container'
        });
    
        dialogRef.afterClosed().subscribe(result => {
            if (result) {
                this.dbService.storeEncryptionKey(this.userId, result);
                this.router.navigate(['/join-room']);
            } else {
                console.log("UserKeyGuard: Dialog closed without token");
            }
        });
    }
}