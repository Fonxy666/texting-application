import { Component } from '@angular/core';
import { RegistrationRequest } from '../model/RegistrationRequest';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';

@Component({
  selector: 'app-registration',
  templateUrl: './registration.component.html',
  styleUrl: './registration.component.css'
})
export class RegistrationComponent {
    constructor(public http: HttpClient, private router: Router) { }

    SendRegistration(data: RegistrationRequest) {
        this.http.post('http://localhost:5000/Auth/Register', data)
        .subscribe((response) => {
            console.log(response);
            if (response) {
                this.router.navigate(['/']);
            }
        });
    }
}
