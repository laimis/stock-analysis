dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover --filter="Category!=Integration"

reportgenerator -reports:tests/coretests/coverage.opencover.xml;tests/storagetests/coverage.opencover.xml;tests/storage.redis.tests/coverage.opencover.xml -targetdir:coverage