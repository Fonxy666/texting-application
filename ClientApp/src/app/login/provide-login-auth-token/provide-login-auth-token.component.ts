import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';

@Component({
  selector: 'app-provide-login-auth-token',
  templateUrl: './provide-login-auth-token.component.html',
  styleUrl: './provide-login-auth-token.component.css'
})
export class ProvideLoginAuthTokenComponent implements OnInit {
    constructor(private fb: FormBuilder, private router: Router) { }
    
    token!: FormGroup;
    
    ngOnInit(): void {
        this.token = this.fb.group({
            token: ['', Validators.required]
        });
    }

    @Output()
    SendToken: EventEmitter<string> = new EventEmitter<string>();

    OnFormSubmit() {
        const token = this.token.get('token')?.value
        this.SendToken.emit(token);
    }

    HandleBackClick() {
        this.router.navigate(['/login']);
    }
}
