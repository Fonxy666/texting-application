import { Component, OnInit } from '@angular/core';
import { CookieService } from 'ngx-cookie-service';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrl: './home.component.css'
})
export class HomeComponent implements OnInit {
    constructor(private cookieService: CookieService) { }

    myImage: string = "./assets/images/backgroundpng.png";
    isSunActive: boolean = true;
    isMoonActive: boolean = false;
    displayHomeText: string = "";
    animation: boolean = true;

    ngOnInit() {
        this.animation = this.cookieService.get("Animation") == "True";
        
        this.toggleImageClasses();

        setInterval(() => {
            this.toggleImageClasses();
        }, 10000);
    }
    
    toggleImageClasses() {
        this.isSunActive = !this.isSunActive;
    }
}
