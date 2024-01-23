import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrl: './home.component.css'
})
export class HomeComponent implements OnInit {
    myImage: string = "./assets/images/mountains-3270710.svg";
    isSunActive: boolean = true;
    isMoonActive: boolean = false;
    displayHomeText: string = "";

    ngOnInit() {
        setTimeout(() => {
            this.toggleImageClasses();

            setInterval(() => {
                this.toggleImageClasses();
            }, 10000);
        }, 15500);

        setTimeout(() => {
            this.animateText();
        }, 5000); 
    }
    
    toggleImageClasses() {
        this.isSunActive = !this.isSunActive;
    }

    animateText() {
        const originalText = "Hello! :) <br> Welcome to Textinger.<br> Don't be alone,<br> chat with someone!";
        let currentIndex = 0;
    
        const intervalId = setInterval(() => {
            this.displayHomeText += originalText[currentIndex];
            currentIndex++;
        
            if (currentIndex === originalText.length) {
                clearInterval(intervalId);
            }
        }, 50);
      }
}
