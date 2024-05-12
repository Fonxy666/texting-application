import { HttpClient } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { MessageService } from 'primeng/api';

@Component({
  selector: 'app-new-password-request',
  templateUrl: './new-password-request.component.html',
  styleUrl: '../../../styles.css',
  providers: [ MessageService ]
})
export class NewPasswordRequestComponent implements OnInit {
    constructor(private route: ActivatedRoute, private http: HttpClient, private messageService: MessageService) {}

    idParam: string = "";
    emailParam: string = "";
    validCode: boolean = false;
    isLoading: boolean = false;

    ngOnInit(): void {
        this.isLoading = true;
        this.idParam = this.route.snapshot.params['id'];
        this.emailParam = this.route.snapshot.params['email'];

        this.examineCode();

        setTimeout(() => {
            this.isLoading = false;
            console.log(this.validCode);
            if (!this.validCode) {
                this.messageService.add({ severity: 'error', summary: 'Error', detail: 'The code expired, try get another one.' });
            }
        }, 500);
    }

    examineCode() {
        this.http.get(`/api/v1/User/ExaminePasswordResetLink?email=${this.emailParam}&resetId=${this.idParam}`)
        .subscribe((response: any) => {
            console.log(response === true);
            if (response == true) {
                this.validCode = true;
            }
        },
        (error) => {
            console.error("An error occurred:", error);
        });
    }
}
