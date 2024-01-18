import { Component, inject } from '@angular/core';
import { LoginRequest } from '../Model/LoginRequest';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})

export class LoginComponent {
    http: HttpClient = inject(HttpClient);

    CreateTask(data: LoginRequest) {
        this.http.post('http://localhost:5003/Auth/Login', data)
        .subscribe((response) => {
            console.log(response);
        });
    }
}
