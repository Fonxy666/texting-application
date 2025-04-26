import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { HttpClient } from '@angular/common/http';
import { UserService } from '../../../services/user-service/user.service';
import { ChangeEmailRequest } from '../../../model/user-credential-requests/user-credentials-requests';

@Component({
  selector: 'app-generate-email-change-request',
  templateUrl: './generate-email-change-request.component.html',
  styleUrls: ['../../../../styles.css', '../profile.component.css', './generate-email-change-request.component.css']
})
export class GenerateEmailChangeRequestComponent implements OnInit {
    constructor(
        private fb: FormBuilder,
        private http: HttpClient, 
        private messageService: MessageService,
        private userService: UserService
    ) { }
    
    changeEmailRequest!: FormGroup;
    email: string = "";

    ngOnInit(): void {
        this.userService.email$.subscribe(email => {
            this.email = email;
        });
        
        this.changeEmailRequest = this.fb.group({
            newEmail: ['', [Validators.required, Validators.email]]
        });
    }

    OnFormSubmit() {
        if (this.changeEmailRequest.valid) {
            const changeEmailRequest: ChangeEmailRequest = {
                oldEmail: this.email,
                newEmail: this.changeEmailRequest.get('newEmail')?.value
            }

            if (changeEmailRequest.newEmail === changeEmailRequest.oldEmail) {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'This is your actual e-mail. Try with another.'
                });
                return;
            }

            this.userService.changeEmail(changeEmailRequest)
            this.http.patch(`/api/v1/User/ChangeEmail`, changeEmailRequest, { withCredentials: true})
            .subscribe((response: any) => {
                if (response) {
                    this.messageService.add({
                        severity: 'info',
                        summary: 'Info',
                        detail: 'Your e-mail changed.',
                        styleClass: 'ui-toast-message-info'
                    });
                    this.userService.setEmail(changeEmailRequest.newEmail);
                    this.changeEmailRequest.reset();
                }
            }, 
            (error) => {
                if (error.status === 400) {
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Error',
                        detail: 'This new e-mail is already in use. Try with another.'
                    });
                }
            })
        }
    }
}