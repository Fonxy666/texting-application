import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { LoginAuthTokenRequest } from '../../model/auth-requests/LoginAuthTokenRequest';
import { LoginRequest } from '../../model/auth-requests/LoginRequest';
import { TokenValidatorRequest } from '../../model/auth-requests/TokenValidatorRequest';
import { Observable } from 'rxjs';
import { AuthResponse } from '../../model/responses/auth-responses.model';
import { EmailAndUserNameRequest, RegistrationRequest } from '../../model/auth-requests/RegistrationRequest';

@Injectable({
  providedIn: 'root'
})
export class AuthService {

    constructor(
        private http: HttpClient,
    ) { }

    login(form: LoginAuthTokenRequest): Observable<AuthResponse<string>> {
        return this.http.post<AuthResponse<string>>("/api/v1/Auth/Login", form, { withCredentials: true})
    }

    sendLoginToken(form: LoginRequest): Observable<AuthResponse<string>> {
        return this.http.post<AuthResponse<string>>(`/api/v1/Auth/SendLoginToken`, form, { withCredentials: true });
    }

    sendVerifyEmail(form: EmailAndUserNameRequest): Observable<AuthResponse<string>> {
        const headers = new HttpHeaders({ 
            'Content-Type': 'application/json'
        });

        return this.http.post<AuthResponse<string>>(`/api/v1/Auth/SendEmailVerificationToken`, form, { headers: headers })
    }

    examineVerifyToken(form: TokenValidatorRequest): Observable<AuthResponse<string>> {
        return this.http.post<AuthResponse<string>>(`/api/v1/Auth/ExamineVerifyToken`, form);
    }

    registration(form: RegistrationRequest): Observable<AuthResponse<string>> {
        return this.http.post<AuthResponse<string>>(`/api/v1/Auth/Register`, form);
    }

    logout() : Observable<AuthResponse<string>> {
        return this.http.get<AuthResponse<string>>(`/api/v1/Auth/Logout`, { withCredentials: true });
    }
}
