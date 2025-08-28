import { Component } from '@angular/core';
import { MessageService } from 'primeng/api';

@Component({
  selector: 'app-about-us-page',
  templateUrl: './about-us-page.component.html',
  styleUrl: './about-us-page.component.css',
  providers: [ MessageService ]
})
export class AboutUsPageComponent {
    textingerImage: string = '../../assets/images/cropped_textinger.png';
    computerImage: string = '../../assets/images/computer_image.png';
    securityImage: string = '../../assets/images/developer_infront_of_computers.png';
}
