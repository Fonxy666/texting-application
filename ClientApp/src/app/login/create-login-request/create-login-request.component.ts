import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { LoginRequest } from '../../model/LoginRequest';

@Component({
  selector: 'app-create-login-request',
  templateUrl: './create-login-request.component.html',
  styleUrl: './create-login-request.component.css'
})

export class CreateLoginRequestComponent implements OnInit {
    constructor(private fb: FormBuilder) { }
    
    loginRequest!: FormGroup;
    
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
}
