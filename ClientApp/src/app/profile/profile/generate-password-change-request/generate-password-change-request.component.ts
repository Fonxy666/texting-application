import { Component, ElementRef, OnInit, Renderer2, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { passwordValidator, passwordMatchValidator } from '../../../validators/ValidPasswordValidator';
import { MessageService } from 'primeng/api';
import { UserService } from '../../../services/user-service/user.service';
import { ChangePasswordRequestForUser } from '../../../model/user-credential-requests/user-credentials-requests';
import { Router } from '@angular/router';

@Component({
  selector: 'app-generate-password-change-request',
  templateUrl: './generate-password-change-request.component.html',
  styleUrls: ['./generate-password-change-request.component.css', '../../../../styles.css', '../profile.component.css']
})
export class GeneratePasswordChangeRequestComponent implements OnInit {
    @ViewChild('oldPasswordInput') oldPasswordInput!: ElementRef;
    @ViewChild('newPasswordInput') newPasswordInput!: ElementRef;
    @ViewChild('repeatNewPasswordInput') repeatNewPasswordInput!: ElementRef;
    @ViewChild('passwordToggleIconForOldPassword') passwordToggleIconForOldPassword!: ElementRef;
    @ViewChild('passwordToggleIconForNewPassword') passwordToggleIconForNewPassword!: ElementRef;
    @ViewChild('passwordToggleIconForNewPasswordRepeat') passwordToggleIconForNewPasswordRepeat!: ElementRef;

    showOldPassword: boolean = false;
    showNewPassword: boolean = false;
    showRepeatNewPassword: boolean = false;

    constructor(
        private fb: FormBuilder,
        private router: Router,
        private userService: UserService,
        private messageService: MessageService,
        private renderer: Renderer2
    ) { }

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

    togglePasswordVisibility(event: Event, type: string): void {
        event.preventDefault();
        switch (type) {
            case "old":
                this.showOldPassword = !this.showOldPassword;
                this.updatePasswordField(this.oldPasswordInput, this.passwordToggleIconForOldPassword, this.showOldPassword);
                break;

            case "new":
                this.showNewPassword = !this.showNewPassword;
                this.updatePasswordField(this.newPasswordInput, this.passwordToggleIconForNewPassword, this.showNewPassword);
                break;

            case "repeat":
                this.showRepeatNewPassword = !this.showRepeatNewPassword;
                this.updatePasswordField(this.repeatNewPasswordInput, this.passwordToggleIconForNewPasswordRepeat, this.showRepeatNewPassword);
                break;
        
            default:
                break;
        }
    }

    updatePasswordField(passwordInput: ElementRef, toggleIcon: ElementRef, showPassword: boolean): void {
        const inputType = showPassword ? 'text' : 'password';
        const iconClassToAdd = showPassword ? 'fa-eye' : 'fa-eye-slash';
        const iconClassToRemove = showPassword ? 'fa-eye-slash' : 'fa-eye';

        this.renderer.setAttribute(passwordInput.nativeElement, 'type', inputType);
        this.renderer.removeClass(toggleIcon.nativeElement, iconClassToRemove);
        this.renderer.addClass(toggleIcon.nativeElement, iconClassToAdd);
    }

    OnFormSubmit() {
        const changePasswordRequest: ChangePasswordRequestForUser = {
            oldPassword: this.changePasswordRequest.get('oldPassword')?.value,
            password: this.changePasswordRequest.get('password')?.value
        };

        this.userService.changePassword(changePasswordRequest)
        .subscribe((response) => {
            if (response.isSuccess) {
                this.messageService.add({
                    severity: 'info',
                    summary: 'Info',
                    detail: 'Your password changed.',
                    styleClass: 'ui-toast-message-info'
                });

                this.changePasswordRequest.reset();

                this.showOldPassword = false;
                this.showNewPassword = false;
                this.showRepeatNewPassword = false;
            } else {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: response.error?.message
                });
            }
        },
        (error) => {
            if (error.error.includes("Account is locked")) {
                this.router.navigate(['/'], { queryParams: { logout: 'true' } });
                console.log("OKE");
            }
            console.log(error);
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: error.error
            });
        });
    }

    passwordMatchValidator(group: FormGroup) {
        const password = group.get('password')?.value;
        const passwordrepeat = group.get('passwordrepeat')?.value;
        return password === passwordrepeat ? null : { passwordMismatch: true };
    }
}