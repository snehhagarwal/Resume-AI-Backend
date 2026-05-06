FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/ResumeAI.AI.API/ResumeAI.AI.API.csproj", "src/ResumeAI.AI.API/"]
COPY ["src/ResumeAI.Shared/ResumeAI.Shared.csproj", "src/ResumeAI.Shared/"]
RUN dotnet restore "src/ResumeAI.AI.API/ResumeAI.AI.API.csproj"
COPY . .
WORKDIR "/src/src/ResumeAI.AI.API"
RUN dotnet build "ResumeAI.AI.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ResumeAI.AI.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ResumeAI.AI.API.dll"]
