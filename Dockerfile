FROM mcr.microsoft.com/dotnet/core/aspnet:3.0.0-alpine3.9
WORKDIR /app
COPY ./publish /app
ENTRYPOINT ["dotnet", "web.dll"]