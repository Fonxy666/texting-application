FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

COPY appsettings.json ./

#environment variables:
ENV IssueAudience=${IssueAudience}
ENV ConnectionString=${ConnectionString}
ENV IssueSign=${IssueSign}
ENV AdminEmail=${AdminEmail}
ENV AdminUserName=${AdminUserName}
ENV AdminPassword=${AdminPassword}
ENV DeveloperEmail=${DeveloperEmail}
ENV DeveloperPassword=${DeveloperPassword}
ENV GoogleClientId=${GoogleClientId}
ENV GoogleClientSecret=${GoogleClientSecret}
ENV FacebookClientId=${FacebookClientId}
ENV FacebookClientSecret=${FacebookClientSecret}

COPY --from=build-env /app/out .

ENV ASPNETCORE_URLS=http://+:80

ENTRYPOINT ["dotnet", "MessageAppServer.dll"]
