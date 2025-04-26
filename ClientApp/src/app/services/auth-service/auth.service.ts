import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthResponse } from '../../model/responses/auth-responses.model';
import { EmailAndUserNameRequest, LoginAuthTokenRequest, LoginRequest, RegistrationRequest, TokenValidatorRequest } from '../../model/auth-requests/auth-requests';

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
        return this.http.post<AuthResponse<string>>(`/api/v1/Auth/SendEmailVerificationToken`, form )
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
