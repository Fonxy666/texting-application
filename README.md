# Texting application - Full Stack Messaging Application

Textinger is a full-stack ASP.NET core application designed to chat with your friends in the web application in an MSSQL database with an Angular(typescript) Frontend Server.

#Project Status
ONGOING project!

# Backend Installation Instructions
- Install the .NET SDK 8.0.204 (.NET 8.0.4)
- Install dependencies.
- Set up the database connections, and other needs through the `secrets.json` file.

# About the Application
- Register to the application.
- You can configure your profile if you click on your avatar in the top right corner, than under the profile menu.
- You can disable animation/appear as an anonym person if you click on your avatar in the top right corner, than under the settings menu.
- You can register a new room, or join to an existing one under the "Connect to a room" menu.
- In the chat room, you can delete or modify your already sent messages if you click on the '3 dot' next to your message.

# The backend is built on the ASP.NET 8 framework, with the main goals of being/having:
- Secure: for example: 2FA, refresh tokens
- Transparent
- Easy to use
- Object-Oriented
- MSSQL database

- #The frontend is powered by Angular using 17.0 version to provide a fast, clean and interactive user interface:
- Also transparent
- Secure (for example, users need to verify themself to join to a room/ chat with friends)
- Bootstrap (for responsive view, so users can use the application in mobiles as well)
- Easy to use: everything is easy and logical

# Security
The application implements secure practices such as JWT token, refresh token and hashed password storage, password confirmation, currently running dockerized MSSQL databases, and sensitive data is stored in the `secrets.json` file.

# Configuration
On the frontend side, there is no sensitive data stored, so you don't need to specify anything by yourself.

On the backend side, sensitive data is stored in the `secrets.json` file. To set up the application, create a `secrets.json` file in the user profile directory:
- Windows: %APPDATA%\microsoft\UserSecrets<userSecretsId>\secrets.json
- Linux: ~/. microsoft/usersecrets//secrets.json
- Mac: ~/. microsoft/usersecrets//secrets.json

`secrets.json` lookalike:
`{
  "IssueAudience": "{yourIssueAudience}",
  "ConnectionString": "Server={yourserverip},{port};Database={database-name};User Id={your-user-id};Password={your-string-password};MultipleActiveResultSets=true;TrustServerCertificate=True;",
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
  "FrontendPort": "{your frontend port, and ip}"   // like for a localhost angular one: 'http://localhost:4200'
}`

#Tests
  For testing in the backend, you need to define a `testConfiguration.json` -> root/Server/MessageAppServer/testConfiguration.json. These tests will run in a different database, not in the 'real-world' database. This file should look the same as the `secrets.json`.

# Archknowledge
  A special thanks to all contributors and libraries used in this ASP.NET/React project.
