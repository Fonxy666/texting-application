import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';
import { ErrorHandlerService } from '../error-handler-service/error-handler.service';

@Injectable({
  providedIn: 'root'
})
export class MediaService {
  private profilePics: { [userId: string]: string } = {};

  constructor(
    private http: HttpClient,
    private errorHandler: ErrorHandlerService
  ) {}

  getAvatarImage(userId: string): Observable<string> {
    if (this.profilePics[userId]) {
      return of(this.profilePics[userId]);
    }

    return this.http.get(`/api/v1/User/GetImage?userId=${userId}`, { withCredentials: true, responseType: 'blob' })
      .pipe(
        this.errorHandler.handleError401(),
        switchMap((response: Blob) => {
          const reader = new FileReader();
          const result$ = new Observable<string>((observer) => {
            reader.onloadend = () => {
              const result = reader.result as string;
              this.profilePics[userId] = result;
              observer.next(result);
              observer.complete();
            };
          });
          reader.readAsDataURL(response);
          return result$;
        }),
        catchError((error) => {
          console.log(error);
          const defaultPic = "https://ptetutorials.com/images/user-profile.png";
          this.profilePics[userId] = defaultPic;
          return of(defaultPic);
        })
      );
  }

  getProfilePic(userId: string): string {
    return this.profilePics[userId];
  }
}