import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { HomeComponent } from './features/home/home.component';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { CookieService } from 'ngx-cookie-service';
import { NavBarComponent } from './shared/nav-bar/nav-bar.component';
import { LoginComponent } from './features/login/login.component';
import { CreateLoginRequestComponent } from './features/login/create-login-request/create-login-request.component';
import { HttpClientModule } from '@angular/common/http';
import { CreateRegistrationRequestComponent } from './features/registration/create-registration-request/create-registration-request.component';
import { RegistrationComponent } from './features/registration/registration.component';
import { ChatComponent } from './features/chat/chat/chat.component';
import { JoinRoomComponent } from './features/chat/join-room/join-room.component';
import { ProfileComponent } from './features/profile/profile/profile.component';
import { SettingsComponent } from './features/profile/settings/settings.component';
import { ImageCropperModule } from 'ngx-image-cropper';
import { GeneratePasswordChangeRequestComponent } from './features/profile/profile/generate-password-change-request/generate-password-change-request.component';
import { GenerateAvatarChangeRequestComponent } from './features/profile/profile/generate-avatar-change-request/generate-avatar-change-request.component';
import { GenerateEmailChangeRequestComponent } from './features/profile/profile/generate-email-change-request/generate-email-change-request.component';
import { CreateRoomComponent } from './features/chat/create-room/create-room.component';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { LoadingScreenComponent } from './shared/loading-screen/loading-screen.component';
import { ToastModule } from 'primeng/toast';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { ButtonModule } from 'primeng/button';
import { TokenProvideComponent } from './features/token-provide/token-provide.component';
import { ForgotPasswordComponent } from './features/forgot-password/forgot-password.component';
import { NewPasswordRequestComponent } from './features/forgot-password/new-password-request/new-password-request.component';
import { SupportPageComponent } from './features/support-page/support-page.component';
import { AboutUsPageComponent } from './features/about-us-page/about-us-page.component';
import { BackgroundImageComponent } from './shared/background-image/background-image.component';
import { ManageFriendRequestComponent } from './features/profile/profile/manage-friend-request/manage-friend-request.component';
import { MatDialogModule } from '@angular/material/dialog';
import { MessageService } from 'primeng/api';
import { UserKeyGuard } from './core/guards/user-key.guard';
import { GenerateUserKeyChangeRequestComponent } from './features/profile/profile/generate-user-key-change-request/generate-user-key-change-request.component';
import { AiBotComponent } from './features/ai-bot/ai-bot.component';

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
    CreateRoomComponent,
    LoadingScreenComponent,
    TokenProvideComponent,
    ForgotPasswordComponent,
    NewPasswordRequestComponent,
    SupportPageComponent,
    AboutUsPageComponent,
    BackgroundImageComponent,
    ManageFriendRequestComponent,
    GenerateUserKeyChangeRequestComponent,
    AiBotComponent
  ],
  imports: [
    MatDialogModule,
    BrowserModule,
    AppRoutingModule,
    FormsModule,
    HttpClientModule,
    ReactiveFormsModule,
    ImageCropperModule,
    BrowserAnimationsModule,
    ToastModule,
    ButtonModule
  ],
  providers: [
    CookieService,
    MessageService,
    UserKeyGuard,
    provideAnimationsAsync()
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
