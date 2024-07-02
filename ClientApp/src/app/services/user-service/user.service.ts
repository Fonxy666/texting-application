import { Injectable } from '@angular/core';
import { BehaviorSubject, catchError, throwError } from 'rxjs';
import { ErrorHandlerService } from '../error-handler-service/error-handler.service';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { CookieService } from 'ngx-cookie-service';
import { ResetPasswordRequest } from '../../model/user-credential-requests/ResetPasswordRequest';

@Injectable({
  providedIn: 'root'
})
export class UserService {
    constructor(private errorHandler: ErrorHandlerService, private router: Router, private http: HttpClient, private cookieService: CookieService) {
        this.getUsername();
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

    getUsername() {
        this.http.get(`/api/v1/User/GetUsername?userId=${this.cookieService.get("UserId")}`, { withCredentials: true})
        .pipe(
            this.errorHandler.handleError401()
        )
        .subscribe((response: any) => {
            this.userName = response.username;
            if (response.status === 403) {
                this.router.navigate(['/']);
            }
        }, 
        (error) => {
            if (error.status === 403) {
                this.errorHandler.handleError403(error);
            } else {
                console.error("An error occurred:", error);
            }
        });
    };

    forgotPassword(email: string) {
        return this.http.get(`/api/v1/User/SendForgotPasswordToken?email=${email}`)
        .pipe(
            this.errorHandler.handleError401(),
            catchError(error => {
                if (error.status === 403) {
                    this.errorHandler.handleError403(error);
                }
                return throwError(error);
            })
        )
    }

    examinePasswordResetLink(emailParam: string, idParam: string) {
        return this.http.get(`/api/v1/User/ExaminePasswordResetLink?email=${emailParam}&resetId=${idParam}`)
        .pipe(
            this.errorHandler.handleError401(),
            catchError(error => {
                if (error.status === 403) {
                    this.errorHandler.handleError403(error);
                }
                return throwError(error);
            })
        )
    }

    setNewPassword(idParam: string, newPasswordRequest: ResetPasswordRequest) {
        return this.http.post(`/api/v1/User/SetNewPassword?resetId=${idParam}`, newPasswordRequest)
        .pipe(
            this.errorHandler.handleError401(),
            catchError(error => {
                if (error.status === 403) {
                    this.errorHandler.handleError403(error);
                }
                return throwError(error);
            })
        )
    }
}
