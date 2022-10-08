
@REM /p:CollectCoverage=true^
@REM /p:CoverletOutputFormat=opencover^

dotnet test /p:CollectCoverage=true --filter "Category!=Integration&Category!=Postgres"