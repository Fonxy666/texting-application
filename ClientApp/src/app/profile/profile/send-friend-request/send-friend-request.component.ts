import { HttpClient } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { ErrorHandlerService } from '../../../services/error-handler.service';
import { MessageService } from 'primeng/api';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { CookieService } from 'ngx-cookie-service';
import { FriendRequest } from '../../../model/FriendRequest';
import { FriendService } from '../../../services/friend-service/friend.service';

@Component({
  selector: 'app-send-friend-request',
  templateUrl: './send-friend-request.component.html',
  styleUrl: './send-friend-request.component.css',
  providers: [ MessageService ]
})
export class SendFriendRequestComponent implements OnInit {
    constructor(
        private friendService: FriendService,
        private fb: FormBuilder,
        private http: HttpClient,
        private errorHandler: ErrorHandlerService,
        private messageService: MessageService,
        private cookieService: CookieService
    ) { }
    
    friendName!: FormGroup;

    ngOnInit(): void {
        this.friendName = this.fb.group({
            userName: ['', [Validators.required]]
        });
    }

    OnFormSubmit() {
        var friendRequest = new FriendRequest(this.cookieService.get("UserId"), this.friendName.get('userName')?.value);
        
        this.http.post(`/api/v1/User/SendFriendRequest`, friendRequest, { withCredentials: true })
        .pipe(
            this.errorHandler.handleError401()
        )
        .subscribe(
            (response: any) => {
                if (response.message == "Friend request sent.") {
                    this.messageService.add({ severity: 'success', summary: 'Success', detail: `Friend request successfully sent to '${friendRequest.receiver}'.`, styleClass: 'ui-toast-message-success' });
                    this.friendService.sendFriendRequest(friendRequest);
                    this.friendName.reset();
                } else {
                    console.log(response);
                }
            },
            (error: any) => {
                console.log(error);
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
