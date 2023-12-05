FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build-env

RUN apk add --no-cache -U \
    nodejs \
    npm

WORKDIR /app
COPY . /app

RUN dotnet publish ./src/web --self-contained -r linux-musl-x64 -c Release -o /app/out

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine

RUN apk add --no-cache -U \
    curl \
    tzdata

WORKDIR /app
COPY --from=build-env /app/out /app

HEALTHCHECK CMD curl -f http://localhost/health || exit 1

ENTRYPOINT ["dotnet", "web.dll"]