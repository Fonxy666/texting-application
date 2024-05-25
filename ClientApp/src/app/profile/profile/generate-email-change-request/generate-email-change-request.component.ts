import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ChangeEmailRequest } from '../../../model/ChangeEmailRequest';
import { ActivatedRoute } from '@angular/router';
import { MessageService } from 'primeng/api';
import { ErrorHandlerService } from '../../../services/error-handler.service';
import { HttpClient } from '@angular/common/http';
import { UserService } from '../../../services/user.service';

@Component({
  selector: 'app-generate-email-change-request',
  templateUrl: './generate-email-change-request.component.html'
})
export class GenerateEmailChangeRequestComponent implements OnInit {
    constructor(private fb: FormBuilder, private route: ActivatedRoute, private http: HttpClient, private errorHandler: ErrorHandlerService, private messageService: MessageService, private userService: UserService) { }
    
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
            const changeEmailRequest = new ChangeEmailRequest(
                this.email,
                this.changeEmailRequest.get('newEmail')?.value
            );
            console.log(changeEmailRequest);

            if (changeEmailRequest.newEmail === changeEmailRequest.oldEmail) {
                this.messageService.add({ severity: 'error', summary: 'Error', detail: 'This is your actual e-mail. Try with another.' });
                return;
            }

            this.http.patch(`/api/v1/User/ChangeEmail`, changeEmailRequest, { withCredentials: true})
            .pipe(
                this.errorHandler.handleError401()
            )
            .subscribe((response: any) => {
                if (response) {
                    this.messageService.add({ severity: 'info', summary: 'Info', detail: 'Your e-mail changed.', styleClass: 'ui-toast-message-info' });
                    this.userService.setEmail(changeEmailRequest.newEmail);
                    this.changeEmailRequest.reset();
                }
            }, 
            (error) => {
                if (error.status === 403) {
                    this.errorHandler.handleError403(error);
                } else if (error.status === 400) {
                    this.messageService.add({ severity: 'error', summary: 'Error', detail: 'This new e-mail is already in use. Try with another.' });
                }
            })
        }
    }
}