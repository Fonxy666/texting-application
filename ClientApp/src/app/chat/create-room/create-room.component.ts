import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { CookieService } from 'ngx-cookie-service';
import { HttpClient } from '@angular/common/http';
import { CreateRoomRequest } from '../../model/CreateRoomRequest';
import { ErrorHandlerService } from '../../services/error-handler.service';

@Component({
  selector: 'app-create-room',
  templateUrl: './create-room.component.html',
  styleUrl: '../../../styles.css'
})
export class CreateRoomComponent implements OnInit {
    constructor(private fb: FormBuilder, private cookieService: CookieService, private router: Router, private http: HttpClient, private errorHandler: ErrorHandlerService) { }

    myImage: string = "./assets/images/backgroundpng.png";
    boyImage: string = "./assets/images/create_room_image.png"
    createRoomForm!: FormGroup;
    token: string = "";
    animation: boolean = true;
    isSunActive: boolean = true;
    isMoonActive: boolean = false;
    showPassword: boolean = false;

    ngOnInit(): void {
        this.animation = this.cookieService.get("Animation") == "True";

        this.token = this.cookieService.get('Token') ? 
            this.cookieService.get('Token')! : sessionStorage.getItem('Token')!;

        this.createRoomForm = this.fb.group({
            roomName: ['', Validators.required],
            password: ['', Validators.required]
        });
    }

    createForm() {
        return new CreateRoomRequest(
            this.createRoomForm.get('roomName')?.value,
            this.createRoomForm.get('password')?.value,
            this.cookieService.get("UserId")
        )
    }

    sendCreateRoomRequest() {
        this.http.post(`/api/v1/Chat/RegisterRoom`, this.createForm(), { withCredentials: true })
        .pipe(
            this.errorHandler.handleError401()
        )
        .subscribe(
            (response: any) => {
                if (response.success) {
                    this.router.navigate(['join-room']);
                } else {
                    console.log(response.error);
                }
            },
            (error: any) => {
                if (error.status === 403) {
                    this.errorHandler.handleError403(error);
                } else if (error.error && error.error.error === "This room's name already taken.") {
                    this.errorHandler.errorAlert("This room is already taken. Choose another one!");
                } else {
                    console.log(error);
                }
            }
        );
    }

    handleCancel() {
        this.router.navigate(['/join-room']);
    }

    toggleShowPassword() {
        this.showPassword = !this.showPassword;
    }
}
