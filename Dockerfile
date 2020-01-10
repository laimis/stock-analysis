FROM mcr.microsoft.com/dotnet/core/sdk:3.1-alpine3.10 AS build-env

RUN apk add --no-cache -U \
    nodejs \
    npm

RUN mkdir -p /app
COPY . /app
WORKDIR /app

RUN dotnet publish ./src/web -c Release -o /app/out

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-alpine3.10
WORKDIR /app
COPY --from=build-env /app/out /app
ENTRYPOINT ["dotnet", "web.dll"]

HEALTHCHECK CMD curl -f http://localhost/health || exit 1