# syntax=docker/dockerfile:1
# Blazor Server фронт «Дурака» (net10, alpine). Контекст сборки — КОРЕНЬ репозитория:
#   docker build -t thevsakeeper/durak:latest -f PlayingCards.Durak.Blazor/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# csproj + props отдельным слоем — слоевой кэш restore переживает правки кода;
# cache mount на ~/.nuget/packages переживает и инвалидацию слоя (правки зависимостей),
# и общий между сборками (id=nuget — тот же кэш у classic-образа).
# Directory.*.props нужны на restore: версии пакетов заданы централизованно (CPM), без них NU1008.
# Без --locked-mode (в отличие от classic-образа): у net10-Blazor SDK добавляет content-dependent пакет
# Microsoft.AspNetCore.App.Internal.Assets (static web assets), а csproj-only слой его не видит → NU1004.
# Воспроизводимость lock проверяет pre-flight на хосте (dotnet build -p:RestoreLockedMode=true) на полном исходнике.
COPY Directory.Build.props Directory.Packages.props ./
COPY PlayingCards.Durak.Blazor/PlayingCards.Durak.Blazor.csproj PlayingCards.Durak.Blazor/
COPY PlayingCards.Durak.Server/PlayingCards.Durak.Server.csproj PlayingCards.Durak.Server/
COPY PlayingCards.Durak/PlayingCards.Durak.csproj               PlayingCards.Durak/
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet restore PlayingCards.Durak.Blazor/PlayingCards.Durak.Blazor.csproj

COPY PlayingCards.Durak.Blazor/ PlayingCards.Durak.Blazor/
COPY PlayingCards.Durak.Server/ PlayingCards.Durak.Server/
COPY PlayingCards.Durak/        PlayingCards.Durak/
# mount нужен и на publish: пакеты лежат в кэше (не в слое образа), иначе implicit restore не найдёт их
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish PlayingCards.Durak.Blazor/PlayingCards.Durak.Blazor.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
# ICU для полной глобализации (проект завязан на Unicode/кириллицу) — alpine по умолчанию invariant
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
# Идентификатор сборки (git short-SHA + время) — фронт показывает его в «Настройках»,
# чтобы после деплоя глазами убедиться в обновлении. Передаёт deploy-durak.ps1; локально — «dev».
ARG BUILD_VERSION=dev
ARG BUILD_DATE=
ENV BUILD_VERSION=$BUILD_VERSION
ENV BUILD_DATE=$BUILD_DATE
WORKDIR /app
COPY --from=build /app/publish .
# NLog пишет в ${basedir}/logs — отдаём каталог непривилегированному пользователю и уходим с root
RUN mkdir -p /app/logs && chown $APP_UID /app/logs
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "PlayingCards.Durak.Blazor.dll"]
