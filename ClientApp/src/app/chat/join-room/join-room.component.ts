import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { CookieService } from 'ngx-cookie-service';
import { ChatService } from '../../chat.service';
import { HttpClient } from '@angular/common/http';
import { JoinRoomRequest } from '../../model/JoinRoomRequest';

@Component({
  selector: 'app-join-room',
  templateUrl: './join-room.component.html',
  styleUrl: './join-room.component.css'
})

export class JoinRoomComponent implements OnInit {
    constructor(private fb: FormBuilder, private cookieService: CookieService, private router: Router, private chatService: ChatService, private http: HttpClient) { }

    myImage: string = "./assets/images/backgroundpng.png";
    joinRoomForm!: FormGroup;
    isSunActive: boolean = true;
    isMoonActive: boolean = false;

    ngOnInit() : void {
        if (this.cookieService.get('Username').length > 1) {
            this.joinRoomForm = this.fb.group({
                user: [this.cookieService.get('Username'), Validators.required],
                room: ['', Validators.required],
                password: ['', Validators.required]
            });
        } else {
            this.joinRoomForm = this.fb.group({
                user: [sessionStorage.getItem('Username'), Validators.required],
                room: ['', Validators.required],
                password: ['', Validators.required]
            });
        }

        setInterval(() => {
            this.toggleImageClasses();
        }, 10000);
    };

    toggleImageClasses() {
        this.isSunActive = !this.isSunActive;
    }

    createForm() {
        return new JoinRoomRequest(
            this.joinRoomForm.get('user')?.value,
            this.joinRoomForm.get('room')?.value,
            this.joinRoomForm.get('password')?.value
        )
    }

    joinRoom() {
        const data = this.createForm();
        this.http.post('http://localhost:5000/Chat/JoinRoom', data)
        .subscribe(
            (response: any) => {
                if (response.success) {
                    this.setRoomCredentialsAndNavigate(data, response.roomId);
                } else if (response.success === false) {
                    console.log(response.error);
                }
            },
            (error: any) => {
                if (error.error && error.error.error === "Invalid login credentials.") {
                    alert("Invalid room name or password.");
                } else {
                    console.log(error);
                }
            }
        );
    }

    setRoomCredentialsAndNavigate(data: any, roomId: string) {
        sessionStorage.setItem("user", data.UserName);
        sessionStorage.setItem("room", data.RoomName);
        this.chatService.joinRoom(data.UserName, data.RoomName)
        .then(() => {
            this.router.navigate([`/chat/${roomId}`]);
        }).catch((err) => {
            console.log(err);
        })
    }

    goToCreateRoom() {
        this.router.navigate(['create-room']);
    }
}
