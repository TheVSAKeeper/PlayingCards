# syntax=docker/dockerfile:1
# Blazor Server фронт «Дурака» (net10, alpine). Контекст сборки — КОРЕНЬ репозитория:
#   docker build -t thevsakeeper/durak:latest -f PlayingCards.Durak.Blazor/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

# csproj отдельным слоем — кэш restore переживает правки кода
COPY PlayingCards.Durak.Blazor/PlayingCards.Durak.Blazor.csproj PlayingCards.Durak.Blazor/
COPY PlayingCards.Durak.Server/PlayingCards.Durak.Server.csproj PlayingCards.Durak.Server/
COPY PlayingCards.Durak/PlayingCards.Durak.csproj               PlayingCards.Durak/
RUN dotnet restore PlayingCards.Durak.Blazor/PlayingCards.Durak.Blazor.csproj

COPY PlayingCards.Durak.Blazor/ PlayingCards.Durak.Blazor/
COPY PlayingCards.Durak.Server/ PlayingCards.Durak.Server/
COPY PlayingCards.Durak/        PlayingCards.Durak/
RUN dotnet publish PlayingCards.Durak.Blazor/PlayingCards.Durak.Blazor.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
# ICU для полной глобализации (проект завязан на Unicode/кириллицу) — alpine по умолчанию invariant
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "PlayingCards.Durak.Blazor.dll"]
