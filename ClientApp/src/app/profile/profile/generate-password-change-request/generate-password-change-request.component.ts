import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ChangePasswordRequest } from '../../../model/ChangePasswordRequest';
import { CookieService } from 'ngx-cookie-service';
import { passwordValidator, passwordMatchValidator } from '../../../validators/ValidPasswordValidator';

@Component({
  selector: 'app-generate-password-change-request',
  templateUrl: './generate-password-change-request.component.html',
  styleUrl: './generate-password-change-request.component.css'
})
export class GeneratePasswordChangeRequestComponent implements OnInit {
    constructor(private fb: FormBuilder, private cookieService: CookieService) { }

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

    @Output()
    SendPasswordChangeRequest: EventEmitter<ChangePasswordRequest> = new EventEmitter<ChangePasswordRequest>();

    OnFormSubmit() {
        const changePasswordRequest = new ChangePasswordRequest(
            this.cookieService.get("UserId"),
            this.changePasswordRequest.get('oldPassword')?.value,
            this.changePasswordRequest.get('password')?.value
            );
        this.SendPasswordChangeRequest.emit(changePasswordRequest);
    }

    toggleShowPassword() {
        this.showPassword = !this.showPassword;
    }
}
