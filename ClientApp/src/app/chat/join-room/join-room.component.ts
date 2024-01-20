import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { CookieService } from 'ngx-cookie-service';

@Component({
  selector: 'app-join-room',
  templateUrl: './join-room.component.html',
  styleUrl: './join-room.component.css'
})

export class JoinRoomComponent implements OnInit {
    constructor(private fb: FormBuilder, private cookieService: CookieService) { }

    joinRoomForm!: FormGroup;

    ngOnInit() : void {
        this.joinRoomForm = this.fb.group({
            user: [this.cookieService.get('Username'), Validators.required],
            room: ['', Validators.required]
        });
    };

    joinRoom() {
        console.log(this.cookieService.get('Username'));
        console.log(this.joinRoomForm.value);
    }
}
