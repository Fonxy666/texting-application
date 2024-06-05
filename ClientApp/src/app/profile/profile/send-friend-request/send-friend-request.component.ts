import { HttpClient } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { ErrorHandlerService } from '../../../services/error-handler.service';
import { MessageService } from 'primeng/api';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { CookieService } from 'ngx-cookie-service';

@Component({
  selector: 'app-send-friend-request',
  templateUrl: './send-friend-request.component.html',
  styleUrl: './send-friend-request.component.css',
  providers: [ MessageService ]
})
export class SendFriendRequestComponent implements OnInit {
    constructor(private fb: FormBuilder, private http: HttpClient, private errorHandler: ErrorHandlerService, private messageService: MessageService, private cookieService: CookieService) { }
    
    friendName!: FormGroup;

    ngOnInit(): void {
        this.friendName = this.fb.group({
            userName: ['', [Validators.required]]
        });
    }

    OnFormSubmit() {
        var friendRequest = {senderId: this.cookieService.get("UserId"), receiver: this.friendName.get('userName')?.value }
        this.http.post(`/api/v1/User/SendFriendRequest`, friendRequest, { withCredentials: true })
        .pipe(
            this.errorHandler.handleError401()
        )
        .subscribe(
            (response: any) => {
                console.log(response.message == "Friend request sent.")
                if (response.message == "Friend request sent.") {
                    this.messageService.add({ severity: 'success', summary: 'Success', detail: `Friend request successfully sent to '${friendRequest.receiver}'.`, styleClass: 'ui-toast-message-success' });

                    this.friendName.reset();
                }
            },
            (error: any) => {
                if (error.status === 403) {
                    this.errorHandler.handleError403(error);
                } else if (error.status === 400) {
                    this.messageService.add({ severity: 'error', summary: 'Error', detail: `${error.error.message}` });
                } else {
                    console.log(error);
                }
            }
        );
    }
    
}
