import { Component, ElementRef, OnInit, Renderer2, ViewChild } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { passwordMatchValidator, passwordValidator } from '../../validators/ValidPasswordValidator';
import { UserService } from '../../services/user-service/user.service';
import { ResetPasswordRequest } from '../../model/user-credential-requests/user-credentials-requests';

@Component({
  selector: 'app-new-password-request',
  templateUrl: './new-password-request.component.html',
  styleUrls: ['../../../styles.css', './new-password-request.component.css'],
  providers: [ MessageService ]
})
export class NewPasswordRequestComponent implements OnInit {
    @ViewChild('newPasswordInput') newPasswordInput!: ElementRef;
    @ViewChild('repeatNewPasswordInput') repeatNewPasswordInput!: ElementRef;
    @ViewChild('passwordToggleIconForNewPassword') passwordToggleIconForNewPassword!: ElementRef;
    @ViewChild('passwordToggleIconForNewPasswordRepeat') passwordToggleIconForNewPasswordRepeat!: ElementRef;

    constructor(
        private route: ActivatedRoute,
        private messageService: MessageService,
        private fb: FormBuilder,
        private renderer: Renderer2,
        private router: Router,
        private userService: UserService
    ) {}

    idParam: string = "";
    emailParam: string = "";
    validCode: boolean = false;
    isLoading: boolean = false;
    passwordReset!: FormGroup;
    showNewPassword: boolean = false;
    showNewPasswordRepeat: boolean = false;

    ngOnInit(): void {
        this.isLoading = true;
        this.idParam = this.route.snapshot.params['id'];
        this.emailParam = this.route.snapshot.params['email'];

        this.examineCode();

        setTimeout(() => {
            this.isLoading = false;
            if (!this.validCode) {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'The code expired, try get another one.'
                });
            }
        }, 500);

        this.passwordReset = this.fb.group({
            password: ['', [Validators.required, passwordValidator]],
            passwordrepeat: ['', [Validators.required, passwordValidator]]
        }, {
            validators: passwordMatchValidator.bind(this)
        });
    }

    togglePasswordVisibility(event: Event, type: string): void {
        event.preventDefault();
        switch (type) {
            case "new":
                this.showNewPassword = !this.showNewPassword;
                this.updatePasswordField(this.newPasswordInput, this.passwordToggleIconForNewPassword, this.showNewPassword);
                break;

            case "repeat":
                this.showNewPasswordRepeat = !this.showNewPasswordRepeat;
                this.updatePasswordField(this.repeatNewPasswordInput, this.passwordToggleIconForNewPasswordRepeat, this.showNewPasswordRepeat);
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

    examineCode() {
        this.userService.examinePasswordResetLink(this.emailParam, this.idParam)
        .subscribe((response) => {
            if (response.isSuccess) {
                this.validCode = true;
            } else {
                this.validCode = false;
            }
        },
        (error) => {
            console.error("An error occurred:", error);
        });
    }

    onFormSubmit() {
        const resetRequest: ResetPasswordRequest = {
            Email: this.emailParam,
            ResetCode: this.idParam,
            NewPassword: this.passwordReset.get('password')?.value
        }

        this.userService.setNewPassword(this.idParam, resetRequest)
        .subscribe((response) => {
            if (response.isSuccess) {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: 'Password successfully updated.',
                    styleClass: 'ui-toast-message-success'
                });
                setTimeout(() => {
                    this.router.navigate(['/']);
                }, 2000);
            } else {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: response.message
                });
            }
        },
        (error) => {
            console.error("An error occurred:", error);
        });
    }
}
