import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { IndexedDBService } from '../../../services/db-service/indexed-dbservice.service';
import { CookieService } from 'ngx-cookie-service';
import { MessageService } from 'primeng/api';
import { decryptTokenValidator } from '../../../validators/ValidPasswordValidator';

@Component({
  selector: 'app-generate-user-key-change-request',
  templateUrl: './generate-user-key-change-request.component.html',
  styleUrl: './generate-user-key-change-request.component.css'
})
export class GenerateUserKeyChangeRequestComponent {
    constructor(
        private fb: FormBuilder,
        private dbService: IndexedDBService,
        private cookieService: CookieService,
        private messageService: MessageService
    ) { }
    
    changeUserKeyRequest!: FormGroup;
    email: string = "";

    ngOnInit(): void {        
        this.changeUserKeyRequest = this.fb.group({
            newKey: ['', [Validators.required, decryptTokenValidator]]
        });
    }

    OnFormSubmit() {
        if (this.changeUserKeyRequest.valid) {
            const keyChangeResult = this.dbService.storeEncryptionKey(this.cookieService.get("UserId"), this.changeUserKeyRequest.get('newKey')!.value);

            keyChangeResult
                .then(success => {
                    if (success) {
                        this.messageService.add({
                            severity: 'info',
                            summary: 'Info',
                            detail: 'Successful key change.',
                            styleClass: 'ui-toast-message-info'
                        });
                    } else {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: 'Unsuccessful change, something wrong happened, try again later.'
                        });
                    }
                })
        }
    }
}