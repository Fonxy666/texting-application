import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { EmailAndUserNameRequest, LoginAuthTokenRequest, LoginRequest, RegistrationRequest, TokenValidatorRequest } from '../../model/auth-requests/auth-requests';
import { ServerResponse } from '../../model/responses/shared-response.model';

@Injectable({
  providedIn: 'root'
})
export class AuthService {

    constructor(
        private http: HttpClient,
    ) { }

    login(form: LoginAuthTokenRequest): Observable<ServerResponse<string>> {
        return this.http.post<ServerResponse<string>>("/api/v1/Auth/Login", form, { withCredentials: true})
    }

    sendLoginToken(form: LoginRequest): Observable<ServerResponse<string>> {
        return this.http.post<ServerResponse<string>>(`/api/v1/Auth/SendLoginToken`, form, { withCredentials: true });
    }

    sendVerifyEmail(form: EmailAndUserNameRequest): Observable<ServerResponse<string>> {
        return this.http.post<ServerResponse<string>>(`/api/v1/Auth/SendEmailVerificationToken`, form )
    }

    examineVerifyToken(form: TokenValidatorRequest): Observable<ServerResponse<string>> {
        return this.http.post<ServerResponse<string>>(`/api/v1/Auth/ExamineVerifyToken`, form);
    }

    registration(form: RegistrationRequest): Observable<ServerResponse<string>> {
        return this.http.post<ServerResponse<string>>(`/api/v1/Auth/Register`, form);
    }

    logout() : Observable<ServerResponse<string>> {
        return this.http.get<ServerResponse<string>>(`/api/v1/Auth/Logout`, { withCredentials: true });
    }
}
