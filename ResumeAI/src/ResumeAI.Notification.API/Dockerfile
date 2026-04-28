FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/ResumeAI.Notification.API/ResumeAI.Notification.API.csproj", "src/ResumeAI.Notification.API/"]
COPY ["src/ResumeAI.Shared/ResumeAI.Shared.csproj", "src/ResumeAI.Shared/"]
RUN dotnet restore "src/ResumeAI.Notification.API/ResumeAI.Notification.API.csproj"
COPY . .
WORKDIR "/src/src/ResumeAI.Notification.API"
RUN dotnet build "ResumeAI.Notification.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ResumeAI.Notification.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ResumeAI.Notification.API.dll"]
