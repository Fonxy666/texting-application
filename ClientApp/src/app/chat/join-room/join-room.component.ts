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
    userId: string = this.cookieService.get("UserId");
    userName: string = "";

    ngOnInit() : void {
        this.joinRoomForm = this.fb.group({
            room: ['', Validators.required],
            password: ['', Validators.required]
        })

        setInterval(() => {
            this.toggleImageClasses();
        }, 10000);

        this.getUsername(this.userId);
    };

    toggleImageClasses() {
        this.isSunActive = !this.isSunActive;
    }

    createForm() {
        return new JoinRoomRequest(
            this.joinRoomForm.get('room')?.value,
            this.joinRoomForm.get('password')?.value
        )
    }

    joinRoom() {
        const data = this.createForm();
        this.http.post('https://localhost:7045/Chat/JoinRoom', data, { withCredentials: true })
        .subscribe(
            (response: any) => {
                if (response.success) {
                    this.setRoomCredentialsAndNavigate(response.roomName, response.roomId);
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

    getUsername(user: any) {
        this.http.get(`https://localhost:7045/User/getUsername/${user}`, { withCredentials: true})
        .subscribe((response: any) => {
            this.userName = response.username;
            console.log(this.userName);
        }, 
        (error) => {
            if (error.status === 400) {
                alert("Invalid username or password.");
            } else {
                console.error("An error occurred:", error);
            }
        });
    }

    setRoomCredentialsAndNavigate(roomName: any, roomId: string) {
        this.chatService.joinRoom(this.userName, roomName)
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
