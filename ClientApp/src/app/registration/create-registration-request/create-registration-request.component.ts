import { Component, EventEmitter, Output } from '@angular/core';
import { FormBuilder, FormGroup,  Validators } from '@angular/forms';
import { RegistrationRequest } from '../../model/RegistrationRequest';
import { ImageCroppedEvent } from 'ngx-image-cropper';
import { DomSanitizer } from '@angular/platform-browser';
import { passwordValidator, passwordMatchValidator } from '../../validators/ValidPasswordValidator';

@Component({
  selector: 'app-create-registration-request',
  templateUrl: './create-registration-request.component.html',
  styleUrl: './create-registration-request.component.css'
})

export class CreateRegistrationRequestComponent {
    constructor(private fb: FormBuilder, private sanitizer: DomSanitizer) { }
    
    showPassword: boolean = false;
    registrationRequest!: FormGroup;
    profilePic: string = "";
    imageChangedEvent: any = '';
    croppedImage: any = '';
    
    ngOnInit(): void {
        this.registrationRequest = this.fb.group({
            email: ['', [Validators.required, Validators.email]],
            username: ['', Validators.required],
            password: ['', [Validators.required, passwordValidator]],
            passwordrepeat: ['', [Validators.required, passwordValidator]],
            phoneNumber: ['', Validators.required]
        }, {
            validators: passwordMatchValidator.bind(this)
        });
    }
    
    @Output()
    SendRegistrationRequest: EventEmitter<RegistrationRequest> = new EventEmitter<RegistrationRequest>();

    OnFormSubmit() {
        const registrationRequest = new RegistrationRequest(
            this.registrationRequest.get('email')?.value,
            this.registrationRequest.get('username')?.value,
            this.registrationRequest.get('password')?.value,
            this.profilePic,
            this.registrationRequest.get('phoneNumber')?.value
        );
        this.SendRegistrationRequest.emit(registrationRequest);
    }

    toggleShowPassword() {
        this.showPassword = !this.showPassword;
    }

    fileChangeEvent(event: any): void {
        this.imageChangedEvent = event;
    }

    imageCropped(event: ImageCroppedEvent) {
        this.croppedImage = this.sanitizer.bypassSecurityTrustUrl(event.base64!!);
        this.onProfilePicChange(event);
    }

    async onProfilePicChange(event: ImageCroppedEvent) {
        const base64data = event.blob instanceof Blob ? await this.getBase64FromBlob(event.blob) : '';
        this.profilePic = base64data;
    }
    
    getBase64FromBlob(blob: Blob): Promise<string> {
        return new Promise((resolve, reject) => {
            const reader = new FileReader();
            reader.readAsDataURL(blob);
            reader.onloadend = () => resolve(reader.result as string);
            reader.onerror = (error) => reject(error);
        });
    }
}
