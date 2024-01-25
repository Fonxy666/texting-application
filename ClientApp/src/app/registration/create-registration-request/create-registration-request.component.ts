import { Component, EventEmitter, Output } from '@angular/core';
import { FormBuilder, FormGroup,  Validators } from '@angular/forms';
import { RegistrationRequest } from '../../model/RegistrationRequest';
import { ImageCroppedEvent, LoadedImage } from 'ngx-image-cropper';
import { DomSanitizer } from '@angular/platform-browser';

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
            email: ['', Validators.required],
            username: ['', Validators.required],
            password: ['', Validators.required],
            passwordrepeat: ['', Validators.required]
        }, {
            validators: this.passwordMatchValidator.bind(this)
        });
    }
    
    @Output()
    SendRegistrationRequest: EventEmitter<RegistrationRequest> = new EventEmitter<RegistrationRequest>();

    OnFormSubmit() {
        const registrationRequest = new RegistrationRequest(
            this.registrationRequest.get('email')?.value,
            this.registrationRequest.get('username')?.value,
            this.registrationRequest.get('password')?.value,
            this.profilePic
        );
        this.SendRegistrationRequest.emit(registrationRequest);
    }

    passwordMatchValidator(group: FormGroup): { [key: string]: boolean } | null {
        const password = group.get('password')?.value;
        const passwordRepeat = group.get('passwordrepeat')?.value;
    
        return password === passwordRepeat ? null : { 'passwordMismatch': true };
    }

    toggleShowPassword() {
        this.showPassword = !this.showPassword;
    }

    onProfilePicChange(event: any) {
        const selectedFile = event.target.files[0];
    
        if (selectedFile) {
            const reader = new FileReader();
            reader.onload = (e: any) => {
                const previewUrl = e.target.result;
                this.profilePic = previewUrl;
            };
            reader.readAsDataURL(selectedFile);
        }
    }

    fileChangeEvent(event: any): void {
        this.imageChangedEvent = event;
    }
    imageCropped(event: ImageCroppedEvent) {
      this.croppedImage = this.sanitizer.bypassSecurityTrustUrl(event.objectUrl!);
      // event.blob can be used to upload the cropped image
    }
    imageLoaded(image: LoadedImage) {
        // show cropper
    }
    cropperReady() {
        // cropper ready
    }
    loadImageFailed() {
        // show message
    }
}
