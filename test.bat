
@REM /p:CollectCoverage=true^
@REM /p:CoverletOutputFormat=opencover^

dotnet test /p:CollectCoverage=true --filter "Category!=Integration&Category!=Postgres"
    

reportgenerator -reports:tests/coretests/coverage.opencover.xml;tests/storagetests/coverage.opencover.xml -targetdir:coverage