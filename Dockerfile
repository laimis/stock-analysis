FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine AS build-env

RUN apk add --no-cache -U \
    nodejs \
    npm

RUN mkdir -p /app
COPY . /app
WORKDIR /app

RUN dotnet publish ./src/web --self-contained -r linux-musl-x64 -c Release -o /app/out

FROM mcr.microsoft.com/dotnet/core/aspnet:5.0-alpine

WORKDIR /app
COPY --from=build-env /app/out /app

RUN apk add --no-cache -U \
    curl \
    tzdata

HEALTHCHECK CMD curl -f http://localhost/health || exit 1

ENTRYPOINT ["dotnet", "web.dll"]