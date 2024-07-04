import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { ErrorHandlerService } from '../error-handler-service/error-handler.service';
import { HttpClient } from '@angular/common/http';
import { CookieService } from 'ngx-cookie-service';
import { ResetPasswordRequest } from '../../model/user-credential-requests/ResetPasswordRequest';
import { ChangePasswordRequestForUser } from '../../model/user-credential-requests/ChangePasswordRequestForUser';
import { ChangeEmailRequest } from '../../model/user-credential-requests/ChangeEmailRequest';

@Injectable({
  providedIn: 'root'
})
export class UserService {
    constructor(
        private errorHandler: ErrorHandlerService,
        private http: HttpClient,
        private cookieService: CookieService
    ) {
        this.getUsername(this.cookieService.get("UserId")).subscribe(userName => {
            this.userName = userName.username;
        });
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

    getUserCredentials(): Observable<any> {
        return this.errorHandler.handleErrors(
            this.http.get(`/api/v1/User/GetUserCredentials`, { withCredentials: true })
        )
    }

    getUsername(userId: string): Observable<any> {
        return this.errorHandler.handleErrors(
            this.http.get(`/api/v1/User/GetUsername?userId=${userId}`, { withCredentials: true})
        )
    };

    forgotPassword(email: string): Observable<any> {
        return this.errorHandler.handleErrors(
            this.http.get(`/api/v1/User/SendForgotPasswordToken?email=${email}`)
        )
    }

    examinePasswordResetLink(emailParam: string, idParam: string): Observable<any> {
        return this.errorHandler.handleErrors(
            this.http.get(`/api/v1/User/ExaminePasswordResetLink?email=${emailParam}&resetId=${idParam}`)
        )
    }

    setNewPassword(idParam: string, newPasswordRequest: ResetPasswordRequest): Observable<any> {
        return this.errorHandler.handleErrors(
            this.http.post(`/api/v1/User/SetNewPassword?resetId=${idParam}`, newPasswordRequest)
        );
    }

    getFriendRequestCount(): Observable<any> {
        return this.errorHandler.handleErrors(
            this.http.get(`/api/v1/User/GetFriendRequestCount`, { withCredentials: true })
        )
    }

    changePassword(form: ChangePasswordRequestForUser): Observable<any> {
        return this. errorHandler.handleErrors(
            this.http.patch(`/api/v1/User/ChangePassword`, form, { withCredentials: true })
        )
    }

    changeEmail(form: ChangeEmailRequest) {
        return this. errorHandler.handleErrors(
            this.http.patch(`/api/v1/User/ChangeEmail`, form, { withCredentials: true})
        )
    }

    changeAvatar(image: string) {
        return this. errorHandler.handleErrors(
            this.http.patch(`/api/v1/User/ChangeAvatar`, image, {
                headers: {
                    'Content-Type': 'application/json'
                },
                withCredentials: true
            })
        )
    }
}
