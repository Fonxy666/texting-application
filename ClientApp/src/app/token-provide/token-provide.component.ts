import { Component, EventEmitter, Inject, Input, OnInit, Optional, Output } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA } from '@angular/material/dialog';

@Component({
    selector: 'app-token-provide',
    templateUrl: './token-provide.component.html',
    styleUrls: ['../../styles.css']
})

export class TokenProvideComponent implements OnInit {
    constructor(
        private fb: FormBuilder,
        @Optional() @Inject(MAT_DIALOG_DATA) public data: any
    ) { }
    
    token!: FormGroup;

    @Input() pageName: string = "";
    @Input() labelName: string = "";
    @Input() inputPlaceholder: string = "";
    @Input() buttonContext: string = "";
    @Input() background: string = "";

    ngOnInit(): void {
        if (this.data) {
            this.pageName = this.data.pageName;
            this.labelName = this.data.labelName;
            this.inputPlaceholder = this.data.inputPlaceholder;
            this.buttonContext = this.data.buttonContext;
            this.background = this.data.background;
        }

        this.token = this.fb.group({
            token: ['', Validators.required]
        });
    }

    @Output()
    SendToken: EventEmitter<string> = new EventEmitter<string>();
    @Output()
    cancelLogin: EventEmitter<void> = new EventEmitter<void>();

    onFormSubmit() {
        const token = this.token.get('token')?.value;
        this.SendToken.emit(token);
    }

    handleBackClick() {
        this.cancelLogin.emit();
    }

    needBackground(): boolean {
        return this.background === 'true';
    }
}