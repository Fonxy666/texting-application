import { Component, ElementRef, OnInit, Renderer2, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { CookieService } from 'ngx-cookie-service';
import { HttpClient } from '@angular/common/http';
import { CreateRoomRequest } from '../../model/CreateRoomRequest';
import { MessageService } from 'primeng/api';
import { ErrorHandlerService } from '../../services/error-handler-service/error-handler.service';

@Component({
  selector: 'app-create-room',
  templateUrl: './create-room.component.html',
  styleUrls: ['../../../styles.css', '../../home/home.component.css', './create-room.component.css'],
  providers: [ MessageService ]
})
export class CreateRoomComponent implements OnInit {
    @ViewChild('passwordInput') passwordInput!: ElementRef;
    @ViewChild('passwordInputToggle') passwordInputToggle!: ElementRef;
    
    constructor(private fb: FormBuilder,
        private cookieService: CookieService,
        private router: Router,
        private http: HttpClient,
        private errorHandler: ErrorHandlerService,
        private messageService: MessageService,
        private renderer: Renderer2
    ) { }

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

    togglePasswordVisibility(event: Event): void {
        event.preventDefault();
        this.showPassword = !this.showPassword;
    
        const inputType = this.showPassword ? 'text' : 'password';
        const iconClassToAdd = this.showPassword ? 'fa-eye' : 'fa-eye-slash';
        const iconClassToRemove = this.showPassword ? 'fa-eye-slash' : 'fa-eye';
    
        this.renderer.setAttribute(this.passwordInput.nativeElement, 'type', inputType);
        this.renderer.removeClass(this.passwordInputToggle.nativeElement, iconClassToRemove);
        this.renderer.addClass(this.passwordInputToggle.nativeElement, iconClassToAdd);
    }

    createForm() {
        return new CreateRoomRequest(
            this.createRoomForm.get('roomName')?.value,
            this.createRoomForm.get('password')?.value
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
                    this.router.navigate(['join-room'], { queryParams: { createRoom: 'true' } });
                } else {
                    console.log(response.error);
                }
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

    handleCancel() {
        this.router.navigate(['/join-room']);
    }

    toggleShowPassword() {
        this.showPassword = !this.showPassword;
    }
}
