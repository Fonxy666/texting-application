import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { CookieService } from 'ngx-cookie-service';
import { ChatService } from '../../chat.service';

@Component({
  selector: 'app-join-room',
  templateUrl: './join-room.component.html',
  styleUrl: './join-room.component.css'
})

export class JoinRoomComponent implements OnInit {
    constructor(private fb: FormBuilder, private cookieService: CookieService, private router: Router, private chatService: ChatService) { }

    joinRoomForm!: FormGroup;

    ngOnInit() : void {
        this.joinRoomForm = this.fb.group({
            user: [this.cookieService.get('Username'), Validators.required],
            room: ['', Validators.required]
        });
    };

    joinRoom() {
        const {user, room} = this.joinRoomForm.value;
        this.chatService.joinRoom(user, room)
        .then(() => {
            this.router.navigate(['/chat']);
        }).catch((err) => {
            console.log(err);
        })
    }
}
