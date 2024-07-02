import { Component, EventEmitter, Output } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { ImageCroppedEvent } from 'ngx-image-cropper';
import { DomSanitizer } from '@angular/platform-browser';
import { MessageService } from 'primeng/api';
import { ChangeAvatarRequest } from '../../../model/user-credential-requests/ChangeAvatarRequest';
import { UserService } from '../../../services/user-service/user.service';

@Component({
  selector: 'app-generate-avatar-change-request',
  templateUrl: './generate-avatar-change-request.component.html',
  styleUrls: ['./generate-avatar-change-request.component.css', '../../../../styles.css']
})
export class GenerateAvatarChangeRequestComponent {
    profilePic: string = "";
    imageChangedEvent: any = '';
    croppedImage: any = '';

    constructor(
        private fb: FormBuilder,
        private sanitizer: DomSanitizer,
        private messageService: MessageService,
        private userService: UserService
    ) { }
    
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
        this.userService.changeAvatar(JSON.stringify(this.profilePic))
        .subscribe((response: any) => {
            if (response && response.status === 'Ok') {
                this.messageService.add({
                    severity: 'info',
                    summary: 'Info',
                    detail: 'Avatar change succeeded :)',
                    styleClass: 'ui-toast-message-info'
                });
            }
        });
    }
}
