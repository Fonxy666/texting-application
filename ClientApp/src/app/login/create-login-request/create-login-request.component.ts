import { Component, ElementRef, EventEmitter, OnInit, Output, Renderer2, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { LoginRequest } from '../../model/auth-requests/LoginRequest';

@Component({
  selector: 'app-create-login-request',
  templateUrl: './create-login-request.component.html',
  styleUrls: ['./create-login-request.component.css', '../../../styles.css']
})

export class CreateLoginRequestComponent implements OnInit {
    @ViewChild('passwordInput') passwordInput!: ElementRef;
    @ViewChild('passwordToggleIcon') passwordToggleIcon!: ElementRef;

    constructor(private fb: FormBuilder, private renderer: Renderer2) { }

    googleIcon: string = "./assets/images/google_icon.png";
    facebookIcon: string = "./assets/images/facebook_image.png";
    loginRequest!: FormGroup;
    showPassword: boolean = false;


    ngOnInit(): void {
        this.loginRequest = this.fb.group({
            username: ['', Validators.required],
            password: ['', Validators.required],
            rememberme: [false, Validators.required]
        });
    }

    togglePasswordVisibility(event: Event): void {
        event.preventDefault();
        this.showPassword = !this.showPassword;
        this.updatePasswordField(this.passwordInput, this.passwordToggleIcon, this.showPassword);
    }

    updatePasswordField(passwordInput: ElementRef, toggleIcon: ElementRef, showPassword: boolean): void {
        const inputType = showPassword ? 'text' : 'password';
        const iconClassToAdd = showPassword ? 'fa-eye' : 'fa-eye-slash';
        const iconClassToRemove = showPassword ? 'fa-eye-slash' : 'fa-eye';

        this.renderer.setAttribute(passwordInput.nativeElement, 'type', inputType);
        this.renderer.removeClass(toggleIcon.nativeElement, iconClassToRemove);
        this.renderer.addClass(toggleIcon.nativeElement, iconClassToAdd);
    }

    @Output()
    SendLoginRequest: EventEmitter<LoginRequest> = new EventEmitter<LoginRequest>();

    OnFormSubmit() {
        const loginRequest = new LoginRequest(
            this.loginRequest.get('username')?.value,
            this.loginRequest.get('password')?.value,
            this.loginRequest.get('rememberme')?.value
            );
        this.SendLoginRequest.emit(loginRequest);
    }

    toggleShowPassword() {
        this.showPassword = !this.showPassword;
    }

    handleGoogleLogin() {
        window.location.href = `/api/v1/Auth/LoginWithGoogle`;
    }

    handleFacebookLogin() {
        window.location.href = `/api/v1/Auth/LoginWithFacebook`;
    }
}
