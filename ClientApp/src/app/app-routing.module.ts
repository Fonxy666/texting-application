import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { LoginComponent } from './login/login.component';
import { RegistrationComponent } from './registration/registration.component';
import { JoinRoomComponent } from './chat/join-room/join-room.component';
import { ChatComponent } from './chat/chat/chat.component';
import { SettingsComponent } from './profile/settings/settings.component';
import { ProfileComponent } from './profile/profile/profile.component';

const routes: Routes = [
    { path: '', component: HomeComponent, title: 'Home' },
    { path: 'login', component: LoginComponent, title: 'Login' },
    { path: 'registration', component: RegistrationComponent, title: 'Registration' },
    { path: 'join-room', component: JoinRoomComponent, title: 'Join room' },
    { path: 'chat', component: ChatComponent, title: 'Chat' },
    { path: 'profile/profile', component: ProfileComponent, title: 'Profile'},
    { path: 'profile/settings', component: SettingsComponent, title: 'Settings'}
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
