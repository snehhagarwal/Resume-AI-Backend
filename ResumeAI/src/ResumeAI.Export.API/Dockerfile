FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Install Chromium dependencies for Puppeteer
RUN apt-get update && apt-get install -y \
    libnss3 \
    libnspr4 \
    libatk1.0-0 \
    libatk-bridge2.0-0 \
    libcups2 \
    libdrm2 \
    libxkbcommon0 \
    libxcomposite1 \
    libxdamage1 \
    libxfixes3 \
    libxrandr2 \
    libgbm1 \
    libasound2 \
    chromium \
    --no-install-recommends \
    && rm -rf /var/lib/apt/lists/*

# Point Puppeteer to the pre-installed Chromium
ENV PUPPETEER_EXECUTABLE_PATH=/usr/bin/chromium

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/ResumeAI.Export.API/ResumeAI.Export.API.csproj", "src/ResumeAI.Export.API/"]
COPY ["src/ResumeAI.Shared/ResumeAI.Shared.csproj", "src/ResumeAI.Shared/"]
RUN dotnet restore "src/ResumeAI.Export.API/ResumeAI.Export.API.csproj"
COPY . .
WORKDIR "/src/src/ResumeAI.Export.API"
RUN dotnet build "ResumeAI.Export.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ResumeAI.Export.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ResumeAI.Export.API.dll"]
