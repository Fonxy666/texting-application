import { Component, OnInit } from '@angular/core';
import { CookieService } from 'ngx-cookie-service';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrl: '../../styles.css'
})
export class HomeComponent implements OnInit {
    constructor(private cookieService: CookieService) { }

    myImage: string = "./assets/images/backgroundpng.png";
    isSunActive: boolean = true;
    isMoonActive: boolean = false;
    displayHomeText: string = "";
    animation: boolean = true;
    isLoading: boolean = false;

    ngOnInit() {
        this.animation = this.cookieService.get("Animation") != "False";
        
        this.toggleImageClasses();

        setInterval(() => {
            this.toggleImageClasses();
        }, 10000);

    }
    
    toggleImageClasses() {
        this.isSunActive = !this.isSunActive;
    }
}
