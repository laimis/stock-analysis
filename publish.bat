dotnet publish -c Release -o publish

docker build -t stock-site .

@powershell ./docker_push.ps1