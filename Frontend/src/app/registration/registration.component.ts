import { Component, inject } from '@angular/core';
import { RegistrationRequest } from '../model/RegistrationRequest';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-registration',
  templateUrl: './registration.component.html',
  styleUrl: './registration.component.css'
})

export class RegistrationComponent {
    http: HttpClient = inject(HttpClient);

    SendRegistration(data: RegistrationRequest) {
        this.http.post('http://localhost:5003/Auth/Register', data)
        .subscribe((response) => {
            console.log(response);
        });
    }
}
