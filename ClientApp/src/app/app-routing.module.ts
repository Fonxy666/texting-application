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

const routes: Routes = [
    { path: '', component: HomeComponent, title: 'Home' },
    { path: 'login', component: LoginComponent, title: 'Login' },
    { path: 'registration', component: RegistrationComponent, title: 'Registration' },
    { path: 'join-room', component: JoinRoomComponent, title: 'Join room' },
    { path: 'chat/:id', component: ChatComponent, title: 'Chat' },
    { path: 'profile/profile', component: ProfileComponent, title: 'Profile'},
    { path: 'profile/settings', component: SettingsComponent, title: 'Settings'},
    { path: 'create-room', component: CreateRoomComponent, title: 'Create room'},
    { path: 'loading', component: LoadingScreenComponent, title: 'Loading'}
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
