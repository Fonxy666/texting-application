FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copy the project file and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# Copy the entire project and build
COPY . ./
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Copy the appsettings.json file
COPY appsettings.json ./

# Copy the Images folder
COPY Images /app/Images

# Set environment variables
ENV IssueAudience=${IssueAudience} \
    ConnectionString=${ConnectionString} \
    IssueSign=${IssueSign} \
    AdminEmail=${AdminEmail} \
    AdminUserName=${AdminUserName} \
    AdminPassword=${AdminPassword} \
    DeveloperEmail=${DeveloperEmail} \
    DeveloperPassword=${DeveloperPassword} \
    GoogleClientId=${GoogleClientId} \
    GoogleClientSecret=${GoogleClientSecret} \
    FacebookClientId=${FacebookClientId} \
    FacebookClientSecret=${FacebookClientSecret} \
    FrontendPort=${FrontendPort}
    
ENV ImageFolderPath=/app/Images

# Copy the compiled application
COPY --from=build-env /app/out .

# Set the ASP.NET Core URL
ENV ASPNETCORE_URLS=http://+:80

# Start the application
ENTRYPOINT ["dotnet", "MessageAppServer.dll"]