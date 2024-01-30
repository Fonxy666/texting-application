import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ChangeEmailRequest } from '../../../model/ChangeEmailRequest';

@Component({
  selector: 'app-generate-email-change-request',
  templateUrl: './generate-email-change-request.component.html',
  styleUrl: './generate-email-change-request.component.css'
})
export class GenerateEmailChangeRequestComponent implements OnInit {
    constructor(private fb: FormBuilder) { }
    
    changeEmailRequest!: FormGroup;
    @Input() email!: string;

    ngOnInit(): void {
        this.changeEmailRequest = this.fb.group({
            newEmail: ['', Validators.required]
        });
    }

    @Output()
    SendPasswordChangeRequest: EventEmitter<ChangeEmailRequest> = new EventEmitter<ChangeEmailRequest>();

    OnFormSubmit() {
        const changeEmailRequest = new ChangeEmailRequest(
            this.email,
            this.changeEmailRequest.get('newEmail')?.value,
            'asdasdasd'
            );
        this.SendPasswordChangeRequest.emit(changeEmailRequest);
    }
}
