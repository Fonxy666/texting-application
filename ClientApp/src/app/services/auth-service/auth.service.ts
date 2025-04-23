import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { LoginAuthTokenRequest } from '../../model/auth-requests/LoginAuthTokenRequest';
import { LoginRequest } from '../../model/auth-requests/LoginRequest';
import { TokenValidatorRequest } from '../../model/auth-requests/TokenValidatorRequest';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthService {

    constructor(
        private http: HttpClient,
    ) { }

    login(form: LoginAuthTokenRequest): Observable<any> {
        return this.http.post("/api/v1/Auth/Login", form, { withCredentials: true})
    }

    sendLoginToken(form: LoginRequest): Observable<any> {
        return this.http.post(`/api/v1/Auth/SendLoginToken`, form, { withCredentials: true });
    }

    sendVerifyEmail(form: any): Observable<any> {
        const headers = new HttpHeaders({
            'Content-Type': 'application/json'
        });

        return this.http.post(`/api/v1/Auth/SendEmailVerificationToken`, form, { headers: headers })
    }

    examineVerifyToken(form: TokenValidatorRequest): Observable<any> {
        return this.http.post(`/api/v1/Auth/ExamineVerifyToken`, form);
    }

    registration(form: any): Observable<any> {
        return this.http.post(`/api/v1/Auth/Register`, form);
    }
}
