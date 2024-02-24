import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ChangeAvatarRequest } from '../../../model/ChangeAvatarRequest';
import { ImageCroppedEvent } from 'ngx-image-cropper';
import { DomSanitizer } from '@angular/platform-browser';

@Component({
  selector: 'app-generate-avatar-change-request',
  templateUrl: './generate-avatar-change-request.component.html',
  styleUrl: './generate-avatar-change-request.component.css'
})
export class GenerateAvatarChangeRequestComponent {
    profilePic: string = "";
    imageChangedEvent: any = '';
    croppedImage: any = '';

    constructor(private fb: FormBuilder, private sanitizer: DomSanitizer) { }
    
    changeAvatarRequest!: FormGroup;
    
    ngOnInit(): void {
        this.changeAvatarRequest = this.fb.group({
            email: ['']
        });
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

    @Output()
    SendAvatarChangeRequest: EventEmitter<ChangeAvatarRequest> = new EventEmitter<ChangeAvatarRequest>();

    OnFormSubmit() {
        const changeAvatarRequest = new ChangeAvatarRequest(
            "this.email",
            "this.id"
            );
        this.SendAvatarChangeRequest.emit(changeAvatarRequest);
    }
}
