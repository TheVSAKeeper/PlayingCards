# syntax=docker/dockerfile:1
# Classic MVC фронт «Дурака» (net8, alpine). Контекст сборки — КОРЕНЬ репозитория:
#   docker build -t thevsakeeper/durak-classic:latest -f PlayingCards.Durak.Web/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# csproj + props + lock отдельным слоем — слоевой кэш restore переживает правки кода;
# cache mount на ~/.nuget/packages переживает и инвалидацию слоя (правки зависимостей),
# и общий между сборками (id=nuget — тот же кэш у Blazor-образа).
# Directory.*.props нужны на restore: версии пакетов заданы централизованно (CPM), без них NU1008.
# packages.lock.json + --locked-mode: restore падает при любом расхождении с lock (воспроизводимость).
COPY Directory.Build.props Directory.Packages.props ./
COPY PlayingCards.Durak.Web/PlayingCards.Durak.Web.csproj       PlayingCards.Durak.Web/packages.lock.json       PlayingCards.Durak.Web/
COPY PlayingCards.Durak.Server/PlayingCards.Durak.Server.csproj PlayingCards.Durak.Server/packages.lock.json PlayingCards.Durak.Server/
COPY PlayingCards.Durak/PlayingCards.Durak.csproj               PlayingCards.Durak/packages.lock.json               PlayingCards.Durak/
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet restore PlayingCards.Durak.Web/PlayingCards.Durak.Web.csproj --locked-mode

COPY PlayingCards.Durak.Web/    PlayingCards.Durak.Web/
COPY PlayingCards.Durak.Server/ PlayingCards.Durak.Server/
COPY PlayingCards.Durak/        PlayingCards.Durak/
# mount нужен и на publish: пакеты лежат в кэше (не в слое образа), иначе implicit restore не найдёт их
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish PlayingCards.Durak.Web/PlayingCards.Durak.Web.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
# ICU для полной глобализации (проект завязан на Unicode/кириллицу) — alpine по умолчанию invariant
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
WORKDIR /app
COPY --from=build /app/publish .
# NLog пишет в ${basedir}/logs — отдаём каталог непривилегированному пользователю и уходим с root
RUN mkdir -p /app/logs && chown $APP_UID /app/logs
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "PlayingCards.Durak.Web.dll"]
