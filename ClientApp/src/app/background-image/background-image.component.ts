import { Component, OnInit, AfterViewInit, ViewChild, ElementRef, Renderer2 } from '@angular/core';
import { CookieService } from 'ngx-cookie-service';

@Component({
  selector: 'app-background-image',
  templateUrl: './background-image.component.html',
  styleUrls: ['./background-image.component.css']
})
export class BackgroundImageComponent implements OnInit, AfterViewInit {
    @ViewChild('backgroundVideo') backgroundVideo!: ElementRef<HTMLVideoElement>;
    backgroundVideoSrc: string = "./assets/videos/white_black_video.mp4";
    animation: boolean;

    constructor(private cookiesService: CookieService, private renderer: Renderer2) {
        this.animation = this.cookiesService.get("Animation") == "True";
    }

    ngOnInit() {
        console.log(this.animation);
    }

    ngAfterViewInit() {
        setTimeout(() => {
            const video = this.backgroundVideo?.nativeElement;
            if (video) {
                this.renderer.setProperty(video, 'playbackRate', 1.5);
                const playPromise = video.play();
                
                if (playPromise !== undefined) {
                    playPromise.catch((error) => {
                        console.error('Video play prevented:', error);
                        this.setupUserInteraction(video);
                    });
                }
            } else {
                console.error('Background video element not found');
            }
        }, 0);
    }

    private setupUserInteraction(video: HTMLVideoElement) {
        const userInteractionHandler = () => {
            video.play().then(() => {
                console.log('Video playing after user interaction');
            }).catch((error) => {
                console.error('Video play failed after user interaction:', error);
            });
            document.removeEventListener('click', userInteractionHandler);
            document.removeEventListener('keydown', userInteractionHandler);
        };

        document.addEventListener('click', userInteractionHandler);
        document.addEventListener('keydown', userInteractionHandler);
    }
}