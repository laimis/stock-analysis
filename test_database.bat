dotnet test^
    --filter="Category=Postgres"
    --no-build
    @REM /p:CollectCoverage=true^
    @REM /p:CoverletOutputFormat=opencover^
    
    
@REM remember to set the user id and password here for the tests
@REM I rename this file to test_database_secret.bat once done and have _secret in .gitignore so that it does not get checked in
@REM set DB_CNN=Server^=localhost;Port^=5432;Database^=stockanalysis;User Id^=.....;Password^=.....;Include Error Detail=true

@REM reportgenerator -reports:tests/coretests/coverage.opencover.xml;tests/storagetests/coverage.opencover.xml -targetdir:coverage
