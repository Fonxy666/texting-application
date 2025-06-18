import { Component, ElementRef, OnInit, Renderer2, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { CookieService } from 'ngx-cookie-service';
import { ChatService } from '../../services/chat-service/chat.service';
import { MessageService } from 'primeng/api';
import { CryptoService } from '../../services/crypto-service/crypto.service';
import { catchError, firstValueFrom, from, of, tap } from 'rxjs';
import { IndexedDBService } from '../../services/db-service/indexed-dbservice.service';
import { JoinRoomRequest } from '../../model/room-requests/chat-requests.model';
import { RoomKeyRequest } from '../../model/key-requests/key-requests.model';

@Component({
  selector: 'app-join-room',
  templateUrl: './join-room.component.html',
  styleUrls: ['../../../styles.css', '../../home/home.component.css', './join-room.component.css'],
  providers: [ MessageService ]
})

export class JoinRoomComponent implements OnInit {
    @ViewChild('passwordInput') passwordInput!: ElementRef;
    @ViewChild('passwordInputToggle') passwordInputToggle!: ElementRef;
    
    constructor(
        private fb: FormBuilder,
        private cookieService: CookieService,
        private router: Router,
        private chatService: ChatService,
        private messageService: MessageService,
        private renderer: Renderer2,
        private cryptoService: CryptoService,
        private dbService: IndexedDBService
    ) { }

    backgroundVideo: string = "./assets/videos/white_black_video.mp4";
    joinRoomForm!: FormGroup;
    isSunActive: boolean = true;
    isMoonActive: boolean = false;
    userId: string = this.cookieService.get("UserId");
    userName: string = "";
    animation: boolean = true;
    showPassword: boolean = false;

    ngOnInit() : void {
        this.animation = this.cookieService.get("Animation") == "True";

        setTimeout(() => {
            const urlParams = new URLSearchParams(window.location.search);
            const deleteSuccessParam = urlParams.get('deleteSuccess');
            const roomDeleted = urlParams.get('roomDeleted');
            const createRoomParam = urlParams.get('createRoom');

            if (deleteSuccessParam === 'true') {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: 'Successful deletion.',
                    styleClass: 'ui-toast-message-success'
                });
            } else if (createRoomParam === 'true') {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: 'Successful room creation.',
                    styleClass: 'ui-toast-message-success'
                });
            } else if (roomDeleted === 'true') {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'This room got deleted.'
                });
            }

            const newUrl = window.location.pathname + window.location.search.replace('?deleteSuccess=true', '').replace('?createRoom=true', '').replace('?roomDeleted=true', '');
            history.replaceState({}, document.title, newUrl);
        }, 0);
        
        this.joinRoomForm = this.fb.group({
            room: ['', Validators.required],
            password: ['', Validators.required]
        })

        setInterval(() => {
            this.toggleImageClasses();
        }, 10000);
    };

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

    toggleImageClasses() {
        this.isSunActive = !this.isSunActive;
    };

    createForm() {
        const returningValue: JoinRoomRequest = {
            roomName: this.joinRoomForm.get('room')?.value,
            password: this.joinRoomForm.get('password')?.value
        }

        return returningValue;
    };

    joinRoom() {
        this.chatService.joinToRoom(this.createForm()).subscribe(
            async response => {
                if (response.isSuccess) {
                    const { roomId, roomName } = response.data;
                    const userId = this.cookieService.get("UserId");
                    const usersInRoom = await this.chatService.getUsersInSpecificRoom(roomId);
                    const keyResponse = await firstValueFrom(
                        this.cryptoService.getUserPrivateKeyForRoom(roomId).pipe(
                            catchError(_ => {
                                return of(null);
                            })
                        )
                    );
                    
                    const awaitedUserInputKey = await firstValueFrom(
                        from(this.dbService.getEncryptionKey(userId))
                            .pipe(
                                catchError(() => {
                                    this.messageService.add({
                                        severity: 'error',
                                        summary: 'Error',
                                        detail: 'You did not provide us your token.'
                                    });
                                    return of(null);
                                    })
                            )
                    );
  
                    if (userId && keyResponse?.isSuccess && awaitedUserInputKey) {
                        if (this.chatService.userInRoom()) {
                            this.messageService.add({
                                severity: 'error',
                                summary: 'Error',
                                detail: 'First you need to leave the actual room.'
                            });
                        }
                        
                        this.chatService.setRoomCredentialsAndNavigate(roomName, roomId);
                    } else if (!keyResponse?.isSuccess && usersInRoom > 0) {
                        const request: RoomKeyRequest = {
                            roomId: roomId,
                            connectionId: this.chatService.connection.connectionId!,
                            roomName: roomName
                        }
                        
                        this.chatService.requestSymmetricRoomKey(request);
                    } else if (!keyResponse?.isSuccess && usersInRoom === 0) {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: 'There is no other user in the room. You need to waite for someone.'
                        });
                    }
                }
            },
            error => {
                if (error.error === 'Incorrect login credentials') {
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Error',
                        detail: 'Invalid Roomname or password.'
                    });
                }
            }
        )
    };

    goToCreateRoom() {
        this.router.navigate(['create-room']);
    };
}
