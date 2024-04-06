import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';

@Component({
  selector: 'app-create-email-verification-request',
  templateUrl: './create-email-verification-request.component.html',
  styleUrl: '../../../styles.css'
})
export class CreateEmailVerificationRequestComponent {
    constructor(private fb: FormBuilder, private router: Router) {}

    verificationCode!: FormGroup;
    @Input() user: any;

    ngOnInit(): void {
        this.verificationCode = this.fb.group({
            verifyCode: ['', Validators.required]
        });
    }

    @Output()
    SendVerificationCode: EventEmitter<String> = new EventEmitter<String>();

    OnFormSubmit() {
        const registrationRequest = new String(
            this.verificationCode.get('verifyCode')?.value
        );
        this.SendVerificationCode.emit(registrationRequest);
    }

    HandleBackClick() {
        this.router.navigate(['/']);
    }
}
