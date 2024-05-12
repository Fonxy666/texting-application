import { HttpClient } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { MessageService } from 'primeng/api';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { passwordMatchValidator, passwordValidator } from '../../validators/ValidPasswordValidator';

@Component({
  selector: 'app-new-password-request',
  templateUrl: './new-password-request.component.html',
  styleUrl: '../../../styles.css',
  providers: [ MessageService ]
})
export class NewPasswordRequestComponent implements OnInit {
    constructor(private route: ActivatedRoute, private http: HttpClient, private messageService: MessageService, private fb: FormBuilder) {}

    idParam: string = "";
    emailParam: string = "";
    validCode: boolean = false;
    isLoading: boolean = false;
    showPassword: boolean = false;
    passwordReset!: FormGroup;

    ngOnInit(): void {
        this.isLoading = true;
        this.idParam = this.route.snapshot.params['id'];
        this.emailParam = this.route.snapshot.params['email'];

        // this.examineCode();

        setTimeout(() => {
            this.isLoading = false;
            if (!this.validCode) {
                this.messageService.add({ severity: 'error', summary: 'Error', detail: 'The code expired, try get another one.' });
            }
        }, 500);

        this.passwordReset = this.fb.group({
            password: ['', [Validators.required, passwordValidator]],
            passwordrepeat: ['', [Validators.required, passwordValidator]]
        }, {
            validators: passwordMatchValidator.bind(this)
        });
    }

    examineCode() {
        this.http.get(`/api/v1/User/ExaminePasswordResetLink?email=${this.emailParam}&resetId=${this.idParam}`)
        .subscribe((response: any) => {
            console.log(response === true);
            if (response == true) {
                this.validCode = true;
            } else {
                this.validCode = false;
            }
        },
        (error) => {
            console.error("An error occurred:", error);
        });
    }

    onFormSubmit() {
        const password = this.passwordReset.get('password')?.value
        this.http.get(`/api/v1/User/SetNewPassword?email=${this.emailParam}&password=${password}`)
        .subscribe((response: any) => {
            if (response == true) {
                this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Password successfully updated.', styleClass: 'ui-toast-message-success' });
            }
        },
        (error) => {
            console.error("An error occurred:", error);
        });
    }

    toggleShowPassword() {
        this.showPassword = !this.showPassword;
    }
}
