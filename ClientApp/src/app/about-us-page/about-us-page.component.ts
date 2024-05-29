import { Component } from '@angular/core';
import { MessageService } from 'primeng/api';

@Component({
  selector: 'app-about-us-page',
  templateUrl: './about-us-page.component.html',
  styleUrl: './about-us-page.component.css',
  providers: [ MessageService ]
})
export class AboutUsPageComponent {
    myImage: string = '../../assets/images/T-extinger.png';
}
