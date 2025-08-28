import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { defer, Observable, of, throwError } from 'rxjs';
import { mergeMap, retryWhen, delay, take, catchError } from 'rxjs/operators';

@Injectable({
  providedIn: 'root',
})
export class ErrorHandlerService {
    constructor(private router: Router) {}

    private handleError403(error: any): void {
        if (error.status === 403) {
            alert('Token expired, you need to log in again.');
            this.router.navigate(['/login']);
        }
    }

    private handleError401() {
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

    handleErrors<T>(source: Observable<T>): Observable<T> {
        return source.pipe(
            this.handleError401(),
            catchError(error => {
                if (error.status === 403) {
                    this.handleError403(error);
                }
                return throwError(error);
            })
        );
    }
}