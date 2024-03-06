import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { defer, Observable, of } from 'rxjs';
import { mergeMap, retryWhen, delay, take } from 'rxjs/operators';

@Injectable({
  providedIn: 'root',
})
export class ErrorHandlerService {
    constructor(private router: Router) {}

    handleError403(error: any): void {
        if (error.status === 403) {
            alert('Token expired, you need to log in again.');
            this.router.navigate(['/login']);
        }
    }

    handleError401() {
        let retryCount = 0;

        return (errors: Observable<any>) =>
            errors.pipe(
                retryWhen((errorObservable) =>
                    errorObservable.pipe(
                    mergeMap((error: any) => {
                        if (error.status === 401 && retryCount < 3) {
                        retryCount++;
                        return defer(() => of(error));
                        }
                        throw error;
                    }),
                        delay(1000),
                        take(3)
                    )
                )
            );
    }

    errorAlert (text: string) {
        alert(text);
    }
}