import { Component, ElementRef, EventEmitter, Input, Output, Renderer2, ViewChild } from '@angular/core';
import { FormGroup } from '@angular/forms';

@Component({
    selector: 'app-room-form',
    templateUrl: './room-form.component.html',
    styleUrls: ['../../../styles.css', '../../features/home/home.component.css'],
})
export class RoomFormComponent {
    @ViewChild('passwordInput') passwordInput!: ElementRef;
    @ViewChild('passwordInputToggle') passwordInputToggle!: ElementRef;
    showPassword: boolean = false;

    backgroundVideo: string = "./assets/videos/white_black_video.mp4";

    constructor(private renderer: Renderer2) { }

    @Input() title!: string;
    @Input() imageSrc!: string;
    @Input() formGroup!: FormGroup;
    @Input() submitLabel = 'Submit';
    @Input() cancelLabel = 'Cancel';
    @Input() showCancel = true;

    @Output() submit = new EventEmitter<void>();
    @Output() cancel = new EventEmitter<void>();

    togglePasswordVisibility(event: Event) {
        event.preventDefault();
        this.showPassword = !this.showPassword;
    
        const inputType = this.showPassword ? 'text' : 'password';
        const iconClassToAdd = this.showPassword ? 'fa-eye' : 'fa-eye-slash';
        const iconClassToRemove = this.showPassword ? 'fa-eye-slash' : 'fa-eye';
    
        this.renderer.setAttribute(this.passwordInput.nativeElement, 'type', inputType);
        this.renderer.removeClass(this.passwordInputToggle.nativeElement, iconClassToRemove);
        this.renderer.addClass(this.passwordInputToggle.nativeElement, iconClassToAdd);
    }

    onSubmit() {
        this.submit.emit();
    }

    onCancel() {
        this.cancel.emit();
    }
}
