dotnet clean

rmdir /S /Q publish

dotnet publish .\src\web\web.csproj -c Release -o publish

docker build -t stock-site .

@powershell ./docker_push.ps1