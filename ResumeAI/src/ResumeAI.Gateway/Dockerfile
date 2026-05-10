FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/ResumeAI.Gateway/ResumeAI.Gateway.csproj", "src/ResumeAI.Gateway/"]
RUN dotnet restore "src/ResumeAI.Gateway/ResumeAI.Gateway.csproj"
COPY . .
WORKDIR "/src/src/ResumeAI.Gateway"
RUN dotnet build "ResumeAI.Gateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ResumeAI.Gateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ResumeAI.Gateway.dll"]
