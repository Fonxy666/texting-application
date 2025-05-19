import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { ErrorHandlerService } from '../error-handler-service/error-handler.service';
import { HttpClient } from '@angular/common/http';
import { CookieService } from 'ngx-cookie-service';
import { GetUserCredentials, UserResponse } from '../../model/responses/user-responses.model';
import { ChangeEmailRequest, ChangePasswordRequestForUser, ResetPasswordRequest } from '../../model/user-credential-requests/user-credentials-requests';
import { AuthResponse } from '../../model/responses/auth-responses.model';

@Injectable({
  providedIn: 'root'
})
export class UserService {
    constructor(
        private errorHandler: ErrorHandlerService,
        private http: HttpClient,
        private cookieService: CookieService
    ) {
        let userId = this.cookieService.get("UserId");
        
        if (userId !== "") {
            this.getUsername(userId).subscribe(response => {
                if (response.isSuccess) {
                    this.userName = response.data;
                } else {
                    this.userName = "Username"
                }
            });
        }
    }

    private emailSubject = new BehaviorSubject<string>('');
    private imageSubject = new BehaviorSubject<string>('');
    email$ = this.emailSubject.asObservable();
    image$ = this.imageSubject.asObservable();
    userName = "";

    setEmail(email: string) {
        this.emailSubject.next(email);
    }

    setImage(image: string) {
        this.imageSubject.next(image);
    }

    getUserCredentials(): Observable<UserResponse<GetUserCredentials>> {
        return this.errorHandler.handleErrors(
            this.http.get<UserResponse<GetUserCredentials>>(`/api/v1/User/GetUserCredentials`, { withCredentials: true })
        )
    }

    getUsername(userId: string): Observable<UserResponse<string>> {
        return this.errorHandler.handleErrors(
            this.http.get<UserResponse<string>>(`/api/v1/User/GetUsername?userId=${userId}`, { withCredentials: true})
        )
    };

    forgotPassword(email: string): Observable<AuthResponse<string>> {
        return this.errorHandler.handleErrors(
            this.http.get<AuthResponse<string>>(`/api/v1/User/SendForgotPasswordToken?email=${email}`)
        )
    }

    examinePasswordResetLink(emailParam: string, idParam: string): Observable<AuthResponse<void>> {
        return this.errorHandler.handleErrors(
            this.http.get<AuthResponse<void>>(`/api/v1/User/ExaminePasswordResetLink?email=${emailParam}&resetId=${idParam}`)
        )
    }

    setNewPassword(idParam: string, newPasswordRequest: ResetPasswordRequest): Observable<AuthResponse<void>> {
        return this.errorHandler.handleErrors(
            this.http.post<AuthResponse<void>>(`/api/v1/User/SetNewPassword?resetId=${idParam}`, newPasswordRequest)
        );
    }

    getFriendRequestCount(): Observable<UserResponse<number>> {
        return this.errorHandler.handleErrors(
            this.http.get<UserResponse<number>>(`/api/v1/User/GetFriendRequestCount`, { withCredentials: true })
        )
    }

    changePassword(form: ChangePasswordRequestForUser): Observable<UserResponse<void>> {
        return this. errorHandler.handleErrors(
            this.http.patch<UserResponse<void>>(`/api/v1/User/ChangePassword`, form, { withCredentials: true })
        )
    }

    changeEmail(form: ChangeEmailRequest): Observable<UserResponse<string>> {
        return this. errorHandler.handleErrors(
            this.http.patch<UserResponse<string>>(`/api/v1/User/ChangeEmail`, form, { withCredentials: true})
        )
    }

    changeAvatar(image: string): Observable<UserResponse<string>> {
        return this. errorHandler.handleErrors(
            this.http.patch<UserResponse<string>>(`/api/v1/User/ChangeAvatar`, image, {
                headers: {
                    'Content-Type': 'application/json'
                },
                withCredentials: true
            })
        )
    }
}
