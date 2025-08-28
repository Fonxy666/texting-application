import { HttpClient } from '@angular/common/http';
import { Component } from '@angular/core';
import { FormGroup } from '@angular/forms';

@Component({
  selector: 'app-ai-bot',
  templateUrl: './ai-bot.component.html',
  styleUrl: './ai-bot.component.css'
})
export class AiBotComponent {
  message: string = '';

  constructor(private http: HttpClient) {}

  sendMessage() {
    this.http.post('http://localhost:8000/ai-chat', {text: this.message})
      .subscribe(response => {
        console.log('Response from FastAPI:', response);
      });
  }
}
