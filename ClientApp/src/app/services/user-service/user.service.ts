import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class UserService {
  constructor() { }

  private emailSubject = new BehaviorSubject<string>('');
    private imageSubject = new BehaviorSubject<string>('');
    email$ = this.emailSubject.asObservable();
    image$ = this.imageSubject.asObservable();

    setEmail(email: string) {
        this.emailSubject.next(email);
    }

    setImage(image: string) {
        this.imageSubject.next(image);
    }
}
