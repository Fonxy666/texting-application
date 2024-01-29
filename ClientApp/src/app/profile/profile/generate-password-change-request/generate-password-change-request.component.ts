import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ChangePasswordRequest } from '../../../model/ChangePasswordRequest';

@Component({
  selector: 'app-generate-password-change-request',
  templateUrl: './generate-password-change-request.component.html',
  styleUrl: './generate-password-change-request.component.css'
})
export class GeneratePasswordChangeRequestComponent implements OnInit {
    constructor(private fb: FormBuilder) { }
    
    changePasswordRequest!: FormGroup;
    @Input() email!: string;
    
    ngOnInit(): void {
        this.changePasswordRequest = this.fb.group({
            email: [''],
            oldPassword: ['', Validators.required],
            newPassword: ['', Validators.required]
        });
    }

    @Output()
    SendPasswordChangeRequest: EventEmitter<ChangePasswordRequest> = new EventEmitter<ChangePasswordRequest>();

    OnFormSubmit() {
        const changePasswordRequest = new ChangePasswordRequest(
            this.email,
            this.changePasswordRequest.get('oldPassword')?.value,
            this.changePasswordRequest.get('newPassword')?.value
            );
        this.SendPasswordChangeRequest.emit(changePasswordRequest);
    }
}
