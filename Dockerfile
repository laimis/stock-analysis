FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build-env

RUN apk add --no-cache -U \
    nodejs \
    npm

# Install Angular CLI globally
RUN npm install -g @angular/cli

WORKDIR /app
COPY . /app

RUN dotnet publish ./src/web --self-contained -r linux-musl-x64 -c Release -o /app/out
RUN dotnet publish ./src/frontend --self-contained -r linux-musl-x64 -c Release

COPY /src/frontend/dist/* /app/out/wwwroot/

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine

RUN apk add --no-cache -U \
    curl \
    tzdata \
    icu-libs

WORKDIR /app
COPY --from=build-env /app/out /app

ENV ASPNETCORE_URLS=http://*:8080
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

ENTRYPOINT ["dotnet", "web.dll"]
