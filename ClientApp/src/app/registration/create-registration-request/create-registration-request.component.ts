import { Component, ElementRef, EventEmitter, Output, Renderer2, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup,  Validators } from '@angular/forms';
import { RegistrationRequest } from '../../model/auth-requests/RegistrationRequest';
import { ImageCroppedEvent } from 'ngx-image-cropper';
import { DomSanitizer } from '@angular/platform-browser';
import { passwordValidator, passwordMatchValidator } from '../../validators/ValidPasswordValidator';
import { Router } from '@angular/router';

@Component({
  selector: 'app-create-registration-request',
  templateUrl: './create-registration-request.component.html',
  styleUrls: ['./create-registration-request.component.css', '../../../styles.css']
})

export class CreateRegistrationRequestComponent {
    @ViewChild('passwordInput') passwordInput!: ElementRef;
    @ViewChild('passwordRepeatInput') passwordRepeatInput!: ElementRef;
    @ViewChild('passwordToggleIcon') passwordToggleIcon!: ElementRef;
    @ViewChild('passwordRepeatToggleIcon') passwordRepeatToggleIcon!: ElementRef;
    
    constructor(private fb: FormBuilder, private sanitizer: DomSanitizer, private router: Router, private renderer: Renderer2) { }
    
    registrationRequest!: FormGroup;
    profilePic: string = "";
    imageChangedEvent: any = '';
    croppedImage: any = '';
    showPassword: boolean = false;
    showPasswordRepeat: boolean = false;
    
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

    togglePasswordVisibility(event: Event, type: string): void {
        event.preventDefault();
        switch (type) {
            case "password":
                this.showPassword = !this.showPassword;
                this.updatePasswordField(this.passwordInput, this.passwordToggleIcon, this.showPassword);
                break;

            case "passwordRepeat":
                this.showPasswordRepeat = !this.showPasswordRepeat;
                this.updatePasswordField(this.passwordRepeatInput, this.passwordRepeatToggleIcon, this.showPasswordRepeat);
                break;
        
            default:
                break;
        }
    }

    updatePasswordField(passwordInput: ElementRef, toggleIcon: ElementRef, showPassword: boolean): void {
        const inputType = showPassword ? 'text' : 'password';
        const iconClassToAdd = showPassword ? 'fa-eye' : 'fa-eye-slash';
        const iconClassToRemove = showPassword ? 'fa-eye-slash' : 'fa-eye';

        this.renderer.setAttribute(passwordInput.nativeElement, 'type', inputType);
        this.renderer.removeClass(toggleIcon.nativeElement, iconClassToRemove);
        this.renderer.addClass(toggleIcon.nativeElement, iconClassToAdd);
    }
    
    @Output()
    SendRegistrationRequest: EventEmitter<RegistrationRequest> = new EventEmitter<RegistrationRequest>();

    onFormSubmit() {
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

    handleCancel() {
        this.router.navigate(['/login']);
    }
}
