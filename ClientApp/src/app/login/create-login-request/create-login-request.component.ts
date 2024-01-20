import { Component, EventEmitter, Output } from '@angular/core';
import { NgForm } from '@angular/forms';
import { LoginRequest } from '../../model/LoginRequest';

@Component({
  selector: 'app-create-login-request',
  templateUrl: './create-login-request.component.html',
  styleUrl: './create-login-request.component.css'
})
export class CreateLoginRequestComponent {
    @Output()
    SendLoginRequest: EventEmitter<LoginRequest> = new EventEmitter<LoginRequest>();

    OnFormSubmit(form: NgForm) {
        this.SendLoginRequest.emit(form.value);
    }
}
