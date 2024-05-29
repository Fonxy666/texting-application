import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { LoginComponent } from './login/login.component';
import { RegistrationComponent } from './registration/registration.component';
import { JoinRoomComponent } from './chat/join-room/join-room.component';
import { ChatComponent } from './chat/chat/chat.component';
import { SettingsComponent } from './profile/settings/settings.component';
import { ProfileComponent } from './profile/profile/profile.component';
import { CreateRoomComponent } from './chat/create-room/create-room.component';
import { LoadingScreenComponent } from './loading-screen/loading-screen.component';
import { ForgotPasswordComponent } from './forgot-password/forgot-password.component';
import { NewPasswordRequestComponent } from './forgot-password/new-password-request/new-password-request.component';
import { AboutUsPageComponent } from './about-us-page/about-us-page.component';
import { SupportPageComponent } from './support-page/support-page.component';
import { GenerateEmailChangeRequestComponent } from './profile/profile/generate-email-change-request/generate-email-change-request.component';
import { GenerateAvatarChangeRequestComponent } from './profile/profile/generate-avatar-change-request/generate-avatar-change-request.component';
import { GeneratePasswordChangeRequestComponent } from './profile/profile/generate-password-change-request/generate-password-change-request.component';
import { AuthGuard } from './auth.guard';

const routes: Routes = [
    { path: '', component: HomeComponent, title: 'Home' },
    { path: 'login', component: LoginComponent, title: 'Login' },
    { path: 'registration', component: RegistrationComponent, title: 'Registration' },
    { path: 'join-room', component: JoinRoomComponent, title: 'Join room', canActivate: [AuthGuard] },
    { path: 'message-room/:id', component: ChatComponent, title: 'Chat', canActivate: [AuthGuard] },
    { path: 'profile/profile', component: ProfileComponent, title: 'Profile', canActivate: [AuthGuard], children: [
        { path: 'emailchange', component: GenerateEmailChangeRequestComponent },
        { path: 'avatarchange', component: GenerateAvatarChangeRequestComponent },
        { path: 'passwordchange', component: GeneratePasswordChangeRequestComponent }
    ]},
    { path: 'profile/settings', component: SettingsComponent, title: 'Settings', canActivate: [AuthGuard] },
    { path: 'create-room', component: CreateRoomComponent, title: 'Create room', canActivate: [AuthGuard] },
    { path: 'loading', component: LoadingScreenComponent, title: 'Loading' },
    { path: 'forgot-password', component: ForgotPasswordComponent, title: 'Reset your password' },
    { path: 'password-reset/:id/:email', component: NewPasswordRequestComponent, title: 'Password reset' },
    { path: 'about-us', component: AboutUsPageComponent, title: 'About us' },
    { path: 'support', component: SupportPageComponent, title: 'Support' },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }