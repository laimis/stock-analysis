dotnet test^
    --filter="Category!=Integration&Category!=Database"
    @REM /p:CollectCoverage=true^
    @REM /p:CoverletOutputFormat=opencover^
    

reportgenerator -reports:tests/coretests/coverage.opencover.xml;tests/storagetests/coverage.opencover.xml -targetdir:coverage