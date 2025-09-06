import { HttpClient } from '@angular/common/http';
import { Component } from '@angular/core';
import { CookieService } from 'ngx-cookie-service';
import { MediaService } from '../../core/services/media-service/media.service';
import { ConnectedUser } from '../../shared/model/chat-models.model';
import { Router } from '@angular/router';

@Component({
    selector: 'app-ai-bot',
    templateUrl: './ai-bot.component.html',
    styleUrl: './ai-bot.component.css'
})
export class AiBotComponent {
    message: string = '';
    userId = this.cookieService.get("UserId")?? "";
	messages: any[] = [];
	connectedUsers: ConnectedUser[] = [{
		userId: this.userId,
		userName: "Fonxy666"
	},
	{
		userId: "Ai_bot_id",
		userName: "Tebot"
	}];

    constructor(
        private http: HttpClient,
        private cookieService: CookieService,
		private mediaService: MediaService,
		public router: Router,
    ) {
		this.mediaService.getAvatarImage(this.userId).subscribe(image => {
			this.avatars[this.userId] = image;
		});
	}


    avatars: { [userId: string]: string } = {};

    sendMessage(message: string) {
		this.messages.push({
			encrypted: false,
			messageData: {
				roomId: "ai-bot-room-id",
				messageId: "message-id",
				senderId: this.userId,
				text: message,
				sendTime: new Date(),
				seenList: [],
				iv: ""
			}
		})

        this.http.post('http://localhost:8000/ai-chat', { text: message })
            .subscribe(response => {
				this.messages.push({
					encrypted: false,
					messageData: {
						roomId: "ai-bot-room-id",
						messageId: "message-id",
						senderId: "ai-bot-id",
						text: response,
						sendTime: new Date(),
						seenList: [],
						iv: ""
					}
				})
            });
    }

	leaveChat(_: boolean) {
		this.router.navigate(['/']);
		this.userId = "";
	}
}
