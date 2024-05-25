import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ChangePasswordRequest } from '../../../model/ChangePasswordRequest';
import { CookieService } from 'ngx-cookie-service';
import { passwordValidator, passwordMatchValidator } from '../../../validators/ValidPasswordValidator';
import { HttpClient } from '@angular/common/http';
import { ErrorHandlerService } from '../../../services/error-handler.service';
import { MessageService } from 'primeng/api';

@Component({
  selector: 'app-generate-password-change-request',
  templateUrl: './generate-password-change-request.component.html',
  styleUrl: './generate-password-change-request.component.css'
})
export class GeneratePasswordChangeRequestComponent implements OnInit {
    constructor(private fb: FormBuilder, private cookieService: CookieService, private http: HttpClient, private errorHandler: ErrorHandlerService, private messageService: MessageService) { }

    showPassword: boolean = false;
    
    changePasswordRequest!: FormGroup;
    
    ngOnInit(): void {
        this.changePasswordRequest = this.fb.group({
            id: [''],
            oldPassword: ['', Validators.required],
            password: ['', [Validators.required, passwordValidator]],
            passwordrepeat: ["", [Validators.required, passwordValidator]]
        }, {
            validators: passwordMatchValidator.bind(this)
        });
    }

    OnFormSubmit() {
        const changePasswordRequest = new ChangePasswordRequest(
            this.cookieService.get("UserId"),
            this.changePasswordRequest.get('oldPassword')?.value,
            this.changePasswordRequest.get('password')?.value
            );

        this.http.patch(`/api/v1/User/ChangePassword`, changePasswordRequest, { withCredentials: true})
        .pipe(
            this.errorHandler.handleError401()
        )
        .subscribe((response: any) => {
            if (response) {
                this.messageService.add({ severity: 'info', summary: 'Info', detail: 'Your password changed.', styleClass: 'ui-toast-message-info' });
            }
        }, 
        (error) => {
            if (error.status === 403) {
                this.errorHandler.handleError403(error);
            } else if (error.status === 400) {
                this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Unsuccessful change, wrong password(s).' });
            }
        });
    }

    toggleShowPassword() {
        this.showPassword = !this.showPassword;
    }
}
