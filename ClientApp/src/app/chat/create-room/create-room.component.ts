import { Component, ElementRef, OnInit, Renderer2, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { CookieService } from 'ngx-cookie-service';
import { MessageService } from 'primeng/api';
import { ChatService } from '../../services/chat-service/chat.service';
import { CryptoService } from '../../services/crypto-service/crypto.service';
import { CreateRoomRequest } from '../../model/room-requests/chat-requests.model';

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
        private renderer: Renderer2,
        public chatService: ChatService,
        private messageService: MessageService,
        private cryptoService: CryptoService
    ) { }

    myImage: string = "./assets/images/backgroundpng.png";
    boyImage: string = "./assets/images/create_room_image.png"
    createRoomForm!: FormGroup;
    token: string = "";
    animation: boolean = true;
    isSunActive: boolean = true;
    isMoonActive: boolean = false;
    showPassword: boolean = false;
    publicKey: string = this.cookieService.get("PublicKey");

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

    async createForm(): Promise<CreateRoomRequest> {
        try {
            const symmetricKey = await this.cryptoService.generateSymmetricKey();
            const cryptoPublicKey = await this.cryptoService.importPublicKeyFromBase64(this.publicKey);
            const encryptedSymmetricKey = await this.cryptoService.encryptSymmetricKey(symmetricKey, cryptoPublicKey);
    
            const createRoomRequest: CreateRoomRequest = {
                roomName: this.createRoomForm.get('roomName')?.value,
                password: this.createRoomForm.get('password')?.value,
                encryptedSymmetricRoomKey: this.cryptoService.bufferToBase64(encryptedSymmetricKey)
            };
    
            return createRoomRequest;
        } catch (error) {
            console.error('Error creating form:', error);
            throw error;
        }
    }

    handleCancel() {
        this.router.navigate(['/join-room']);
    }

    toggleShowPassword() {
        this.showPassword = !this.showPassword;
    }

    async callSendcreateRoomRequest() {
        this.chatService.registerRoom(await this.createForm()).subscribe(
            response => {
                if (response.isSuccess) {
                    this.router.navigate(['join-room'], { queryParams: { createRoom: 'true' } });
                }
            },
            error => {
                if (error.error && error.error.error === "This room's name already taken.") {
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Error',
                        detail: 'This room name is already taken. Choose another one!'
                    });
                } else {
                    console.log(error);
                }
            }   
        )
    }
}
