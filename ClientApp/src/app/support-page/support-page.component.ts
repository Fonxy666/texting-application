import { Component } from '@angular/core';
import { MessageService } from 'primeng/api';

@Component({
  selector: 'app-support-page',
  templateUrl: './support-page.component.html',
  styleUrls: ['./support-page.component.css', '../../styles.css'],
  providers: [ MessageService ]
})
export class SupportPageComponent {
    email: string = 'support@textinger.com';

    constructor(private messageService: MessageService) {}

    copyToClipboard() {
        const emailText = this.email;
    
        const tempInput = document.createElement('input');
        tempInput.value = emailText;
        document.body.appendChild(tempInput);
    
        tempInput.select();
        tempInput.setSelectionRange(0, 99999); // For mobile devices
    
        document.execCommand('copy');
    
        document.body.removeChild(tempInput);
    
        this.messageService.add({ severity: 'info', summary: 'Info', detail: 'Email address copied to clipboard!', styleClass: 'ui-toast-message-info' });
      }
}
