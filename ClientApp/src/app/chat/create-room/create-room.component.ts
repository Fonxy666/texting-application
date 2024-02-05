import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { CookieService } from 'ngx-cookie-service';
import { HttpClient } from '@angular/common/http';
import { CreateRoomRequest } from '../../model/CreateRoomRequest';

@Component({
  selector: 'app-create-room',
  templateUrl: './create-room.component.html',
  styleUrl: './create-room.component.css'
})
export class CreateRoomComponent implements OnInit {
    constructor(private fb: FormBuilder, private cookieService: CookieService, private router: Router, private http: HttpClient) { }

    createRoomForm!: FormGroup;

    ngOnInit(): void {
        this.createRoomForm = this.fb.group({
            roomName: ['', Validators.required],
            password: ['', Validators.required]
        });
    }

    createForm() {
        return new CreateRoomRequest(
            this.createRoomForm.get('roomName')?.value,
            this.createRoomForm.get('password')?.value
        )
    }

    sendCreateRoomRequest() {
        this.http.post('http://localhost:5000/Chat/RegisterRoom', this.createForm())
        .subscribe(
            (response: any) => {
                if (response.success) {
                    this.router.navigate(['join-room']);
                } else {
                    console.log(response.error);
                }
            },
            (error: any) => {
                if (error.error && error.error.error === "This room's name already taken.") {
                    alert("This room is already taken. Choose another one!");
                } else {
                    console.log(error);
                }
            }
        );
    }

    handleCancer() {
        this.router.navigate(['/join-room']);
    }
}
