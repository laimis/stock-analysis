dotnet test --configuration Release --filter="Category=Postgres" 
    @REM /p:CollectCoverage=true^
    @REM /p:CoverletOutputFormat=opencover^
    
@REM reportgenerator -reports:tests/coretests/coverage.opencover.xml;tests/storagetests/coverage.opencover.xml -targetdir:coverage
