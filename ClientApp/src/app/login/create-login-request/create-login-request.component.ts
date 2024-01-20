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

    registrationRequest!: FormGroup;
    
    ngOnInit(): void {
        this.registrationRequest = this.fb.group({
            username: ['', Validators.required],
            password: ['', Validators.required]
        });
    }

    @Output()
    SendLoginRequest: EventEmitter<LoginRequest> = new EventEmitter<LoginRequest>();

    OnFormSubmit() {
        const loginRequest = new LoginRequest(
            this.registrationRequest.get('username')?.value,
            this.registrationRequest.get('password')?.value
        );
        this.SendLoginRequest.emit(loginRequest);
    }
}
