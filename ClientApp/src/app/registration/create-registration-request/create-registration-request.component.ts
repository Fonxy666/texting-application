import { Component, EventEmitter, Output } from '@angular/core';
import { NgForm } from '@angular/forms';
import { RegistrationRequest } from '../../model/RegistrationRequest';

@Component({
  selector: 'app-create-registration-request',
  templateUrl: './create-registration-request.component.html',
  styleUrl: './create-registration-request.component.css'
})
export class CreateRegistrationRequestComponent {
    @Output()
    SendRegistrationRequest: EventEmitter<RegistrationRequest> = new EventEmitter<RegistrationRequest>();

    OnFormSubmit(form: NgForm) {
        this.SendRegistrationRequest.emit(form.value);
    }
}
