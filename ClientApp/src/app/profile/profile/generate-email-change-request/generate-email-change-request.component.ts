import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ChangeEmailRequest } from '../../../model/ChangeEmailRequest';

@Component({
  selector: 'app-generate-email-change-request',
  templateUrl: './generate-email-change-request.component.html'
})
export class GenerateEmailChangeRequestComponent implements OnInit {
    constructor(private fb: FormBuilder) { }
    
    changeEmailRequest!: FormGroup;
    @Input() email!: string;

    ngOnInit(): void {
        this.changeEmailRequest = this.fb.group({
            newEmail: ['', [Validators.required, Validators.email]]
        });
    }

    @Output()
    SendPasswordChangeRequest: EventEmitter<ChangeEmailRequest> = new EventEmitter<ChangeEmailRequest>();

    OnFormSubmit() {
        const changeEmailRequest = new ChangeEmailRequest(
            this.email,
            this.changeEmailRequest.get('newEmail')?.value
            );
        this.SendPasswordChangeRequest.emit(changeEmailRequest);
    }
}
