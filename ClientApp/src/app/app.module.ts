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
import { ProfileComponent } from './profile/profile/profile.component';
import { SettingsComponent } from './profile/settings/settings.component';
import { ImageCropperModule } from 'ngx-image-cropper';

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
    ProfileComponent,
    SettingsComponent,
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    FormsModule,
    HttpClientModule,
    ReactiveFormsModule,
    ImageCropperModule
  ],
  providers: [
    CookieService
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
