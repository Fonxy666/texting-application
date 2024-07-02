import { Injectable } from '@angular/core';
import { ErrorHandlerService } from '../error-handler-service/error-handler.service';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';

@Injectable({
    providedIn: 'root'
})
export class CookiesService {

    constructor(
        private http: HttpClient,
        private errorHandler: ErrorHandlerService,
    ) { }

    changeCookies(param2: string): Observable<any> {
        const headers = new HttpHeaders({
            'Content-Type': 'application/json'
        });
        const params = new HttpParams().set("request", param2);
        
        return this.errorHandler.handleErrors(
            this.http.post(`/api/v1/Cookie/ChangeCookies`, null, { headers: headers, params: params, responseType: 'text', withCredentials: true })
        )
    }
}
