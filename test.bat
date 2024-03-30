
@REM /p:CollectCoverage=true^
@REM /p:CoverletOutputFormat=opencover^

@REM dotnet test /p:CollectCoverage=false --filter "Category!=Integration&Category!=Postgres" -l:"console;verbosity=quiet" --nologo

dotnet test /p:CollectCoverage=false --filter "Category!=Integration&Category!=Postgres" -l:"console;verbosity=quiet" --nologo --configuration Release
