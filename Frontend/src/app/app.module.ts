import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { NavComponent } from './navbar/nav.component';
import { LoginComponent } from './Login/login.component';
import { HomeComponent } from './Home/home.component';
import { FormsModule } from '@angular/forms';
import { CreateLoginRequestComponent } from './Login/create-login-request/create-login-request.component';
import { HttpClientModule } from '@angular/common/http';
import { RegistrationComponent } from './registration/registration.component';
import { CreateRegistrationRequestComponent } from './registration/create-registration-request/create-registration-request.component';

@NgModule({
  declarations: [
    AppComponent,
    NavComponent,
    LoginComponent,
    HomeComponent,
    CreateLoginRequestComponent,
    RegistrationComponent,
    CreateRegistrationRequestComponent
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    FormsModule,
    HttpClientModule
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
