# Сборка приложения
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Сначала копируем только csproj, чтобы слой с restore переиспользовался, пока не менялись зависимости
COPY ["GroupListNet/GroupListNet.Web.Api.csproj", "GroupListNet/"]
COPY ["GroupListNet.Core/GroupListNet.Core.csproj", "GroupListNet.Core/"]
COPY ["GroupListNet.Utils/GroupListNet.Utils.csproj", "GroupListNet.Utils/"]
RUN dotnet restore "GroupListNet/GroupListNet.Web.Api.csproj"

COPY . .
RUN dotnet publish "GroupListNet/GroupListNet.Web.Api.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false

# Рабочий образ
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

ARG INSTALL_CHROMIUM=true
RUN if [ "$INSTALL_CHROMIUM" = "true" ]; then \
        set -eux; \
        apt-get update; \
        for attempt in 1 2 3; do \
            apt-get install -y --no-install-recommends --fix-missing \
                -o Acquire::Retries=5 -o Acquire::http::Timeout=60 \
                chromium chromium-driver fonts-liberation && break; \
            sleep 10; \
            apt-get update; \
        done; \
        chromium --version; \
        chromedriver --version; \
        rm -rf /var/lib/apt/lists/*; \
    fi

WORKDIR /app
COPY --from=build /app/publish .

# NLog пишет логи в каталог приложения, поэтому он должен быть доступен пользователю app на запись
RUN mkdir -p /app/logs && chown -R app:app /app

USER app
EXPOSE 8080

ENTRYPOINT ["dotnet", "GroupListNet.Web.Api.dll"]
