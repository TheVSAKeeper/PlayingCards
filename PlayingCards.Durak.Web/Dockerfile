# syntax=docker/dockerfile:1
# Classic MVC фронт «Дурака» (net8, alpine). Контекст сборки — КОРЕНЬ репозитория:
#   docker build -t thevsakeeper/durak-classic:latest -f PlayingCards.Durak.Web/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# csproj отдельным слоем — кэш restore переживает правки кода
COPY PlayingCards.Durak.Web/PlayingCards.Durak.Web.csproj       PlayingCards.Durak.Web/
COPY PlayingCards.Durak.Server/PlayingCards.Durak.Server.csproj PlayingCards.Durak.Server/
COPY PlayingCards.Durak/PlayingCards.Durak.csproj               PlayingCards.Durak/
RUN dotnet restore PlayingCards.Durak.Web/PlayingCards.Durak.Web.csproj

COPY PlayingCards.Durak.Web/    PlayingCards.Durak.Web/
COPY PlayingCards.Durak.Server/ PlayingCards.Durak.Server/
COPY PlayingCards.Durak/        PlayingCards.Durak/
RUN dotnet publish PlayingCards.Durak.Web/PlayingCards.Durak.Web.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
# ICU для полной глобализации (проект завязан на Unicode/кириллицу) — alpine по умолчанию invariant
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "PlayingCards.Durak.Web.dll"]
