# Textinger - Full Stack Messaging Application

<p align="center">
  <img src="https://raw.githubusercontent.com/Fonxy666/texting-application/main/ClientApp/src/assets/images/textinger.png" width="200" alt="Textinger logo">
</p>

Textinger is a full-stack messaging application built with **ASP.NET Core** and **Angular (TypeScript)**. It allows you to chat with friends securely in real time using a **PostgreSQL** database backend.  

---

## Project Status
**ONGOING** — The project is under active development. The README is being updated for clarity. Feel free to fork and explore the code.  

---

## Quick Overview

- **Home Page**  
  ![Home page](https://github.com/Fonxy666/texting-application/blob/main/GithubImages/home_page_logout.jpg)

- **Login Page**  
  ![Login page](https://github.com/Fonxy666/texting-application/blob/main/GithubImages/login_page.jpg)

- **Chat Page**  
  ![chat page](https://github.com/Fonxy666/texting-application/blob/main/GithubImages/chat_page.jpg)

- **Profile Page**  
  ![profile page](https://github.com/Fonxy666/texting-application/blob/main/GithubImages/profile_page.jpg)

> Note: All “background images” in the app are actually videos for animated effects.

---

## Features

- Register and log in using **username/password**, **Google**, or **Facebook**.
- Configure your profile and privacy settings (disable animations or appear anonymous).
- Join or create a chat room using a **6-digit encryption token**.
- Modify or delete sent messages.
- Send and manage **friend requests**.
- Invite friends to chat rooms.
- **Secure**: 2FA, refresh tokens, hashed passwords, E2E encryption.
- **Scalable**: Microservice architecture; Redis integration planned.
- **Fast**: Optimized database queries for minimal load.
- **Frontend**: Angular 17, Bootstrap responsive design.

---

## Security

- JWT token authentication, refresh tokens, and password confirmation.
- End-to-end encryption: data is encrypted client-side.
- **Asymmetric key vault** stored in a Dockerized **HashiCorp Vault**.
- Sensitive data is kept in `secrets.json`.

---

## Quick Start (Docker)

Spin up the backend, database, and Vault with **one command**:

```bash
git clone https://github.com/Fonxy666/texting-application.git
cd texting-application
docker-compose up -d
```

This will start:

- PostgreSQL databases for UserService and ChatService
- HashiCorp Vault for key storage
- Backend microservices (UserService & ChatService)
You only need to edit secrets.json if you want to customize credentials, OAuth keys, or tokens. Otherwise, default test secrets will work for local testing.

---

## Backend Installation (Manual)
If you prefer to run each service manually:

1. Clone the repository
git clone https://github.com/Fonxy666/texting-application.git
cd texting-application

2. Install dependencies
cd UserService
dotnet restore

cd ../ChatService
dotnet restore

3. Configure secrets

Create secrets.json in the user profile directory:

Windows: %APPDATA%\microsoft\UserSecrets\<userSecretsId>\secrets.json

Linux / Mac: ~/.microsoft/usersecrets/<userSecretsId>/secrets.json

Example: User Service secrets.json:
```
{
  "IssueAudience": "{yourIssueAudience}",
  "UserDbConnectionString": "Host=localhost;Port=5432;Username=postgres;Password={your_password};Database=user_db;SSL Mode=Disable;",
  "IssueSign": "{yourIssueSign}",
  "AdminEmail": "{admin-email}",
  "AdminUserName": "{Admin}",
  "AdminPassword": "{adminPassword123!!!}",
  "DeveloperEmail": "{a hotmail email, where you want to send the e-mails for registration and verification}",
  "DeveloperPassword": "{the e-mails password, for the the program to log in to your existing hotmail account}",
  "GoogleClientId": "{you need to register the application in: 'https://console.cloud.google.com/welcome/new'}",
  "GoogleClientSecret": "after registering the application, you will be able to get the clientsecret as well}",
  "FacebookClientId": "{same as the google,you need to register the app for facebook login, but the url is: ''",
  "FacebookClientSecret": "{same as google, you will get this,too after registering the application in facebookdevs}",
  "FrontendUrlAndPort": "{your frontend port, and ip}"   // like for a localhost angular one: 'http://localhost:4200'
  "HashiCorpToken": you need to create the hashi corp token by visiting the corp ui, see more below. ,
  "HashiCorpAddress": example: "http://127.0.0.1:8200"
}
```
Example: Chat Service secrets.json:
```
{
  "ChatDbConnectionString": "Host=localhost;Port=5433;Username=postgres;Password={your_password};Database=chat_db;SSL Mode=Disable;",
  "IssueAudience": "{yourIssueAudience}",
  "IssueSign": "{yourIssueSign}",
  "AdminEmail": "{admin-email}",
  "AdminUserName": "{Admin}",
  "AdminPassword": "{adminPassword123!!!}",
  "DeveloperEmail": "{a hotmail email, where you want to send the e-mails for registration and verification}",
  "DeveloperPassword": "{the e-mails password, for the the program to log in to your existing hotmail account}",
  "DeveloperAppPassword": "{app password for secure app login}",
  "FrontendUrlAndPort": "{your frontend port, and ip}"   // like for a localhost angular one: 'http://localhost:4200'
  "GrpcUrl" : "the gRPC url and port for the microservice communication" // example: "https://localhost:7100"
}
```

---

## Testing
- Create user-service-test-config.json and chat-service-test-config.json in the respective service roots.
- The configuration should mirror `secrets.json` but with Docker Compose settings.
- Run tests using:
```
cd Services/UserService/UserServuceTests
dotnet test

cd Services/ChatService/ChatServiceTests
dotnet test
```

---

## Technology Stack

- Backend: ASP.NET Core 8, Microservices architecture, PostgreSQL
- Frontend: Angular 17, Bootstrap, TypeScript
- Security: JWT, refresh tokens, E2E encryption, HashiCorp Vault
- Deployment: Dockerized services

---

## Acknowledgements
Special thanks to all contributors and libraries used in this ASP.NET / Angular project.

> Note: This project is a work in progress. Updates and improvements are ongoing.
