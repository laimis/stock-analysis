
@REM /p:CollectCoverage=true^
@REM /p:CoverletOutputFormat=opencover^

dotnet test /p:CollectCoverage=false --filter "Category!=Integration&Category!=Postgres" -l:"console;verbosity=normal"