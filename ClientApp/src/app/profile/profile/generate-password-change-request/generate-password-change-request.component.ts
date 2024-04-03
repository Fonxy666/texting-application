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

    showPassword: boolean = false;
    
    changePasswordRequest!: FormGroup;
    @Input() email!: string;
    
    ngOnInit(): void {
        this.changePasswordRequest = this.fb.group({
            email: [''],
            oldPassword: ['', Validators.required],
            newPassword: ['', Validators.required]
        }, {
            validators: this.validPasswordValidator.bind(this)
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

    validPasswordValidator(group: FormGroup): { [key: string]:boolean } | null {
        const password = group.get('password')?.value;
        const passwordRepeat = group.get('passwordrepeat')?.value;

        if (!password || !passwordRepeat) {
            return null;
        }

        const regex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/;

        if (!regex.test(password)) {
            return { invalidPassword: true };
        }

        return null;
    }

    toggleShowPassword() {
        this.showPassword = !this.showPassword;
    }
}
