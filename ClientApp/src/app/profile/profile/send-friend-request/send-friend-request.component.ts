import { HttpClient } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { ErrorHandlerService } from '../../../services/error-handler.service';
import { MessageService } from 'primeng/api';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { CookieService } from 'ngx-cookie-service';

@Component({
  selector: 'app-send-friend-request',
  templateUrl: './send-friend-request.component.html',
  styleUrl: './send-friend-request.component.css'
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
                console.log(response);
            },
            (error: any) => {
                if (error.status === 403) {
                    this.errorHandler.handleError403(error);
                } else if (error.error && error.error.error === "This room's name already taken.") {
                    this.messageService.add({ severity: 'error', summary: 'Error', detail: 'This room name is already taken. Choose another one!' });
                } else {
                    console.log(error);
                }
            }
        );
    }
    
}
