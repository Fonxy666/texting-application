import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { LoginRequest } from '../../model/LoginRequest';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-create-login-request',
  templateUrl: './create-login-request.component.html',
  styleUrls: ['./create-login-request.component.css', '../../../styles.css']
})

export class CreateLoginRequestComponent implements OnInit {
    constructor(private fb: FormBuilder) { }

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
        window.location.href = `https://localhost:${environment.port}/Auth/LoginWithGoogle`;
    }

    handleFacebookLogin() {
        window.location.href = `https://localhost:${environment.port}/Auth/LoginWithFacebook`;
    }
}
