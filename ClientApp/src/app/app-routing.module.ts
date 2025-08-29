import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { HomeComponent } from './features/home/home.component';
import { LoginComponent } from './features/login/login.component';
import { RegistrationComponent } from './features/registration/registration.component';
import { JoinRoomComponent } from './features/chat/join-room/join-room.component';
import { ChatComponent } from './features/chat/chat/chat.component';
import { SettingsComponent } from './features/profile/settings/settings.component';
import { ProfileComponent } from './features/profile/profile/profile.component';
import { CreateRoomComponent } from './features/chat/create-room/create-room.component';
import { LoadingScreenComponent } from './shared/loading-screen/loading-screen.component';
import { ForgotPasswordComponent } from './features/forgot-password/forgot-password.component';
import { NewPasswordRequestComponent } from './features/forgot-password/new-password-request/new-password-request.component';
import { AboutUsPageComponent } from './features/about-us-page/about-us-page.component';
import { SupportPageComponent } from './features/support-page/support-page.component';
import { GenerateEmailChangeRequestComponent } from './features/profile/profile/generate-email-change-request/generate-email-change-request.component';
import { GenerateAvatarChangeRequestComponent } from './features/profile/profile/generate-avatar-change-request/generate-avatar-change-request.component';
import { GeneratePasswordChangeRequestComponent } from './features/profile/profile/generate-password-change-request/generate-password-change-request.component';
import { ManageFriendRequestComponent } from './features/profile/profile/manage-friend-request/manage-friend-request.component';
import { GenerateUserKeyChangeRequestComponent } from './features/profile/profile/generate-user-key-change-request/generate-user-key-change-request.component';
import { AuthGuard } from './core/guards/auth.guard';
import { UserKeyGuard } from './core/guards/user-key.guard';
import { AiBotComponent } from './features/ai-bot/ai-bot.component';

const routes: Routes = [
    { path: '', component: HomeComponent, title: 'Home' },
    { path: 'login', component: LoginComponent, title: 'Login' },
    { path: 'registration', component: RegistrationComponent, title: 'Registration' },
    { path: 'join-room', component: JoinRoomComponent, title: 'Join room', canActivate: [AuthGuard] },
    { path: 'message-room/:id', component: ChatComponent, title: 'Chat', canActivate: [AuthGuard, UserKeyGuard] },
    { path: 'profile/profile', component: ProfileComponent, title: 'Profile', canActivate: [AuthGuard], children: [
        { path: 'emailchange', component: GenerateEmailChangeRequestComponent },
        { path: 'avatarchange', component: GenerateAvatarChangeRequestComponent },
        { path: 'passwordchange', component: GeneratePasswordChangeRequestComponent },
        { path: 'friendrequest', component: ManageFriendRequestComponent },
        { path: 'userkey', component: GenerateUserKeyChangeRequestComponent }
    ]},
    { path: 'profile/settings', component: SettingsComponent, title: 'Settings', canActivate: [AuthGuard] },
    { path: 'create-room', component: CreateRoomComponent, title: 'Create room', canActivate: [AuthGuard] },
    { path: 'loading', component: LoadingScreenComponent, title: 'Loading' },
    { path: 'forgot-password', component: ForgotPasswordComponent, title: 'Reset your password' },
    { path: 'password-reset/:id/:email', component: NewPasswordRequestComponent, title: 'Password reset' },
    { path: 'about-us', component: AboutUsPageComponent, title: 'About us' },
    { path: 'support', component: SupportPageComponent, title: 'Support' },
    { path: 'ai-bot', component: AiBotComponent, title: 'Ai bot' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }