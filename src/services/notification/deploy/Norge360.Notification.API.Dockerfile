# syntax=docker/dockerfile:1.7

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props Directory.Build.targets Directory.Packages.props Norge360.slnx ./
COPY .nuget/NuGet.Config ./.nuget/NuGet.Config
COPY src/packages/dotnet/src ./src/packages/dotnet/src
COPY src/services/auth/src/Norge360.Auth.Contracts ./src/services/auth/src/Norge360.Auth.Contracts
COPY src/services/notification/src ./src/services/notification/src

RUN dotnet restore src/services/notification/src/Norge360.Notification.API/Norge360.Notification.API.csproj --force-evaluate
RUN dotnet publish src/services/notification/src/Norge360.Notification.API/Norge360.Notification.API.csproj -c Release --no-restore -o /app/publish -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_EnableDiagnostics=0
EXPOSE 8080
COPY --from=build /app/publish ./
USER $APP_UID
ENTRYPOINT ["dotnet", "Norge360.Notification.API.dll"]
