FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/ResumeAI.Auth.API/ResumeAI.Auth.API.csproj", "src/ResumeAI.Auth.API/"]
COPY ["src/ResumeAI.Shared/ResumeAI.Shared.csproj", "src/ResumeAI.Shared/"]
RUN dotnet restore "src/ResumeAI.Auth.API/ResumeAI.Auth.API.csproj"
COPY . .
WORKDIR "/src/src/ResumeAI.Auth.API"
RUN dotnet build "ResumeAI.Auth.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ResumeAI.Auth.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ResumeAI.Auth.API.dll"]
