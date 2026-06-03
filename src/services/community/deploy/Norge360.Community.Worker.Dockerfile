# syntax=docker/dockerfile:1.7
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Build.targets Directory.Packages.props Norge360.slnx ./
COPY .nuget/NuGet.Config ./.nuget/NuGet.Config
COPY src/packages/dotnet/src ./src/packages/dotnet/src
COPY src/services/community/src ./src/services/community/src
RUN dotnet restore src/services/community/src/Norge360.Community.Worker/Norge360.Community.Worker.csproj --force-evaluate
RUN dotnet publish src/services/community/src/Norge360.Community.Worker/Norge360.Community.Worker.csproj -c Release --no-restore -o /app/publish -p:UseAppHost=false
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV DOTNET_EnableDiagnostics=0
COPY --from=build /app/publish ./
USER $APP_UID
ENTRYPOINT ["dotnet", "Norge360.Community.Worker.dll"]
