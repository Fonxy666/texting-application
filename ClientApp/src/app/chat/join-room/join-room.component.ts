import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { CookieService } from 'ngx-cookie-service';
import { ChatService } from '../../services/chat-service/chat.service';
import { HttpClient } from '@angular/common/http';
import { JoinRoomRequest } from '../../model/JoinRoomRequest';
import { ErrorHandlerService } from '../../services/error-handler.service';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-join-room',
  templateUrl: './join-room.component.html',
  styleUrl: '../../../styles.css'
})

export class JoinRoomComponent implements OnInit {
    constructor(private fb: FormBuilder, private cookieService: CookieService, private router: Router, private chatService: ChatService, private http: HttpClient, private errorHandler: ErrorHandlerService) { }

    myImage: string = "./assets/images/backgroundpng.png";
    joinRoomForm!: FormGroup;
    isSunActive: boolean = true;
    isMoonActive: boolean = false;
    userId: string = this.cookieService.get("UserId");
    userName: string = "";
    animation: boolean = true;
    showPassword: boolean = false;

    ngOnInit() : void {
        this.animation = this.cookieService.get("Animation") == "True";
        
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
        this.http.post(`https://localhost:${environment.port}/Chat/JoinRoom`, data, { withCredentials: true })
        .pipe(
            this.errorHandler.handleError401()
        )
        .subscribe(
            (response: any) => {
                if (response.success) {
                    this.setRoomCredentialsAndNavigate(response.roomName, response.roomId);
                }
            },
            (error: any) => {
                if (error.status === 403) {
                    this.errorHandler.handleError403(error);
                } else if (error.error && error.error.error === "Invalid login credentials.") {
                    this.errorHandler.errorAlert("Invalid room name or password.");
                } else if (error.status === 400) {
                    alert("Invalid username or password.");
                } else {
                    console.log(error);
                }
            }
        );
    }

    getUsername(user: any) {
        this.http.get(`https://localhost:${environment.port}/User/GetUsername?userId=${user}`, { withCredentials: true})
        .pipe(
            this.errorHandler.handleError401()
        )
        .subscribe((response: any) => {
            this.userName = response.username;
            if (response.status === 403) {
                alert("Token expired, you need to log in again.");
                this.router.navigate(['/']);;
            }
        }, 
        (error) => {
            if (error.status === 403) {
                this.errorHandler.handleError403(error);
            } else if (error.status === 400) {
                this.errorHandler.errorAlert("Invalid username or password.");
            } else {
                console.error("An error occurred:", error);
            }
        });
    }

    setRoomCredentialsAndNavigate(roomName: any, roomId: string) {
        if (this.cookieService.get("Anonymous") === "True") {
            this.chatService.joinRoom("Anonymous", roomName)
            .then(() => {
                this.router.navigate([`/chat/${roomId}`]);
                sessionStorage.setItem("room", roomName);
                sessionStorage.setItem("user", "Anonymous");
            }).catch((err) => {
                console.log(err);
            })
        } else {
            this.chatService.joinRoom(this.userName, roomName)
            .then(() => {
                this.router.navigate([`/chat/${roomId}`]);
                sessionStorage.setItem("room", roomName);
                sessionStorage.setItem("user", this.userName);
            }).catch((err) => {
                console.log(err);
            })
        }
    }

    goToCreateRoom() {
        this.router.navigate(['create-room']);
    }

    toggleShowPassword() {
        this.showPassword = !this.showPassword;
    }
}
