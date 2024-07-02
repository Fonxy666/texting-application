import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { LoginAuthTokenRequest } from '../../model/auth-requests/LoginAuthTokenRequest';
import { LoginRequest } from '../../model/auth-requests/LoginRequest';
import { TokenValidatorRequest } from '../../model/auth-requests/TokenValidatorRequest';

@Injectable({
  providedIn: 'root'
})
export class AuthService {

    constructor(
        private http: HttpClient,
    ) { }

    login(form: LoginAuthTokenRequest) {
        return this.http.post("/api/v1/Auth/Login", form, { withCredentials: true})
    }

    sendLoginToken(form: LoginRequest) {
        return this.http.post(`/api/v1/Auth/SendLoginToken`, form, { withCredentials: true });
    }

    sendVerifyEmail(form: any) {
        const headers = new HttpHeaders({
            'Content-Type': 'application/json'
        });

        return this.http.post(`/api/v1/Auth/SendEmailVerificationToken`, form, { headers: headers, responseType: 'text' })
    }

    examineVerifyToken(form: TokenValidatorRequest) {
        return this.http.post(`/api/v1/Auth/ExamineVerifyToken`, form);
    }

    registration(form: any) {
        return this.http.post(`/api/v1/Auth/Register`, form);
    }
}
