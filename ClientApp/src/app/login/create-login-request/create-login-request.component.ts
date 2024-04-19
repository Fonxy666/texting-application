import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { LoginRequest } from '../../model/LoginRequest';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-create-login-request',
  templateUrl: './create-login-request.component.html',
  styleUrls: ['./create-login-request.component.css', '../../../styles.css']
})

export class CreateLoginRequestComponent implements OnInit {
    constructor(private fb: FormBuilder, private http: HttpClient) { }

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

    loginWithGoogle() {
        window.location.href = "https://localhost:7045/Auth/LoginWithGoogle";
        // this.http.get('https://localhost:7045/Auth/LoginWithGoogle', { withCredentials: true })
        // .subscribe((response: any) => {
        //     console.log(response);
        // }, 
        // (error: any) => {
        //     if (error.status === 404) {
        //         if (!isNaN(error.error)) {
        //             alert(`Invalid username or password, you have ${5-error.error} tries.`);
        //         } else {
        //             var errorMessage = error.error.split(".")[0] + "." + error.error.split(".")[1];
        //             alert(errorMessage);
        //         }
        //     } else {
        //         console.error("An error occurred:", error);
        //     }
        // });
    }
}
