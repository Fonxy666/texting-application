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
import { GeneratePasswordChangeRequestComponent } from './profile/profile/generate-password-change-request/generate-password-change-request.component';
import { GenerateAvatarChangeRequestComponent } from './profile/profile/generate-avatar-change-request/generate-avatar-change-request.component';
import { GenerateEmailChangeRequestComponent } from './profile/profile/generate-email-change-request/generate-email-change-request.component';
import { CreateEmailVerificationRequestComponent } from './registration/create-email-verification-request/create-email-verification-request.component';
import { CreateRoomComponent } from './chat/create-room/create-room.component';
import { ProvideLoginAuthTokenComponent } from './login/provide-login-auth-token/provide-login-auth-token.component';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { MaterialModule } from './material/material.module';
import { LoadingScreenComponent } from './loading-screen/loading-screen.component';

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
    GeneratePasswordChangeRequestComponent,
    GenerateAvatarChangeRequestComponent,
    GenerateEmailChangeRequestComponent,
    CreateEmailVerificationRequestComponent,
    CreateRoomComponent,
    ProvideLoginAuthTokenComponent,
    LoadingScreenComponent,
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    FormsModule,
    HttpClientModule,
    ReactiveFormsModule,
    ImageCropperModule,
    MaterialModule
  ],
  providers: [
    CookieService,
    provideAnimationsAsync()
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
