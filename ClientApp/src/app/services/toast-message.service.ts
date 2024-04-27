import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
    private messageSource = new Subject<any>();
    message$ = this.messageSource.asObservable();

    constructor() { }

    setMessage(message: any) {
        this.messageSource.next(message);
    }
}