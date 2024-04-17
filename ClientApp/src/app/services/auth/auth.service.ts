import { HttpHeaders, HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({
    providedIn: 'root'
})

export class AuthService {
    private path = "https://localhost:4200";

    constructor(private httpClient: HttpClient) { }

    loginWithGoogle(credentials: string): Observable<any> {
        const header = new HttpHeaders().set('Content-Type', 'application/json');
        return this.httpClient.post<any>(this.path + '/LoginWithGoogle', JSON.stringify(credentials), { headers: header });
    }
}