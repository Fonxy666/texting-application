import { HttpClient } from '@angular/common/http';
import { Component, ElementRef, OnInit, Renderer2, ViewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { MessageService } from 'primeng/api';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { passwordMatchValidator, passwordValidator } from '../../validators/ValidPasswordValidator';
import { ResetPasswordRequest } from '../../model/ResetPasswordRequest';

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

    constructor(private route: ActivatedRoute, private http: HttpClient, private messageService: MessageService, private fb: FormBuilder, private renderer: Renderer2) {}

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
                this.messageService.add({ severity: 'error', summary: 'Error', detail: 'The code expired, try get another one.' });
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
        this.http.get(`/api/v1/User/ExaminePasswordResetLink?email=${this.emailParam}&resetId=${this.idParam}`)
        .subscribe((response: any) => {
            if (response == true) {
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
        const resetRequest = new ResetPasswordRequest(this.emailParam, this.idParam, this.passwordReset.get('password')?.value)

        this.http.post(`/api/v1/User/SetNewPassword?resetId=${this.idParam}`, resetRequest)
        .subscribe((response: any) => {
            if (response == true) {
                this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Password successfully updated.', styleClass: 'ui-toast-message-success' });
            }
        },
        (error) => {
            console.error("An error occurred:", error);
        });
    }
}
