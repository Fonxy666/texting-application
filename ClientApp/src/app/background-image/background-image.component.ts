import { Component } from '@angular/core';
import { CookieService } from 'ngx-cookie-service';

@Component({
  selector: 'app-background-image',
  templateUrl: './background-image.component.html',
  styleUrl: './background-image.component.css'
})
export class BackgroundImageComponent {
    constructor(private cookiesService: CookieService) { }
    backgroundVideo: string = "./assets/videos/white_black_video.mp4";
    animation: boolean = this.cookiesService.get("Animation") == "True";
    
    ngOnInit() {
        const video = document.getElementById('backgroundVideo') as HTMLVideoElement;
        video.playbackRate = 1.5;
        console.log(this.animation);
    }
}
