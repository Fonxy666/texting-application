import { Component, EventEmitter, Output } from '@angular/core';
import { FormBuilder, FormGroup,  Validators } from '@angular/forms';
import { RegistrationRequest } from '../../model/RegistrationRequest';

@Component({
  selector: 'app-create-registration-request',
  templateUrl: './create-registration-request.component.html',
  styleUrl: './create-registration-request.component.css'
})
export class CreateRegistrationRequestComponent {

    constructor(private fb: FormBuilder) { }
    
    showPassword: boolean = false;
    registrationRequest!: FormGroup;
    
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
            this.registrationRequest.get('password')?.value
        );
        console.log(registrationRequest);
        // this.SendRegistrationRequest.emit(registrationRequest);
    }

    passwordMatchValidator(group: FormGroup): { [key: string]: boolean } | null {
        const password = group.get('password')?.value;
        const passwordRepeat = group.get('passwordrepeat')?.value;
    
        return password === passwordRepeat ? null : { 'passwordMismatch': true };
    }

    toggleShowPassword() {
        this.showPassword = !this.showPassword;
    }
}
