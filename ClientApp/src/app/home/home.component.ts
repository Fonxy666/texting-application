import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrl: './home.component.css'
})
export class HomeComponent implements OnInit {
    myImage: string = "./assets/images/backgroundpng.png";
    isSunActive: boolean = true;
    isMoonActive: boolean = false;
    displayHomeText: string = "";

    ngOnInit() {
        this.toggleImageClasses();

        setInterval(() => {
            this.toggleImageClasses();
        }, 10000);
    }
    
    toggleImageClasses() {
        this.isSunActive = !this.isSunActive;
    }
}
