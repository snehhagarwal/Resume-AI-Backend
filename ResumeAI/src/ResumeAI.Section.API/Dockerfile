FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/ResumeAI.Section.API/ResumeAI.Section.API.csproj", "src/ResumeAI.Section.API/"]
COPY ["src/ResumeAI.Shared/ResumeAI.Shared.csproj", "src/ResumeAI.Shared/"]
RUN dotnet restore "src/ResumeAI.Section.API/ResumeAI.Section.API.csproj"
COPY . .
WORKDIR "/src/src/ResumeAI.Section.API"
RUN dotnet build "ResumeAI.Section.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ResumeAI.Section.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ResumeAI.Section.API.dll"]
