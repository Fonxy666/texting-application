import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';

@Component({
  selector: 'app-token-provide',
  templateUrl: './token-provide.component.html',
  styleUrl: '../../styles.css'
})
export class TokenProvideComponent implements OnInit {
    constructor(private fb: FormBuilder) { }
    
    token!: FormGroup;
    @Input() pageName: string = "";
    @Input() labelName: string = "";
    @Input() buttonContext: string = "";
    
    ngOnInit(): void {
        this.token = this.fb.group({
            token: ['', Validators.required]
        });
    }

    @Output()
    SendToken: EventEmitter<string> = new EventEmitter<string>();
    @Output()
    cancelLogin: EventEmitter<void> = new EventEmitter<void>();

    onFormSubmit() {
        const token = this.token.get('token')?.value
        this.SendToken.emit(token);
    }

    handleBackClick() {
        this.cancelLogin.emit();
    }
}
