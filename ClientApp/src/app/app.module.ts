import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { HomeComponent } from './home/home.component';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { CookieService } from 'ngx-cookie-service';
import { NavBarComponent } from './nav-bar/nav-bar.component';
import { LoginComponent } from './login/login.component';
import { CreateLoginRequestComponent } from './login/create-login-request/create-login-request.component';
import { HttpClientModule } from '@angular/common/http';
import { CreateRegistrationRequestComponent } from './registration/create-registration-request/create-registration-request.component';
import { RegistrationComponent } from './registration/registration.component';
import { ChatComponent } from './chat/chat/chat.component';
import { JoinRoomComponent } from './chat/join-room/join-room.component';

@NgModule({
  declarations: [
    AppComponent,
    HomeComponent,
    NavBarComponent,
    LoginComponent,
    CreateLoginRequestComponent,
    CreateRegistrationRequestComponent,
    RegistrationComponent,
    ChatComponent,
    JoinRoomComponent,
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    FormsModule,
    HttpClientModule,
    ReactiveFormsModule
  ],
  providers: [
    CookieService
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
