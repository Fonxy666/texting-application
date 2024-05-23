import { Component, OnInit, AfterViewInit, ViewChild, ElementRef, Renderer2 } from '@angular/core';
import { CookieService } from 'ngx-cookie-service';

@Component({
  selector: 'app-background-image',
  templateUrl: './background-image.component.html',
  styleUrls: ['./background-image.component.css']
})
export class BackgroundImageComponent implements AfterViewInit {
    @ViewChild('backgroundVideo') backgroundVideo!: ElementRef<HTMLVideoElement>;
    backgroundVideoSrc: string = "./assets/videos/white_black_video.mp4";
    animation: boolean;

    constructor(private cookiesService: CookieService, private renderer: Renderer2) {
        this.animation = this.cookiesService.get("Animation") == "True";
    }

    ngAfterViewInit() {
        setTimeout(() => {
            const video = this.backgroundVideo?.nativeElement;
            if (video) {
                this.renderer.setProperty(video, 'playbackRate', 1.5);
            } else {
                console.error('Background video element not found');
            }
        }, 0);
    }
}