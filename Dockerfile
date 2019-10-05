FROM mcr.microsoft.com/dotnet/core/sdk:3.0.100-alpine3.9 AS build-env
WORKDIR /app

# copy everything and publish (restore, build, test, publish)
RUN dotnet build -c Release
RUN dotnet publish ./src/web -c Release -o ../out

# build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.0.0-alpine3.9
WORKDIR /app
COPY --from=build-env /app/out ./
ENTRYPOINT ["dotnet", "web.dll"]