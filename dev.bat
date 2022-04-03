REM fully functioning app needs teh following environment variables set:

REM set storage=[postgres|memory]
REM set IEXClientToken=.... # iex client token you can obtain from https://www.iexcloud.io/
REM set DB_CNN=Server=...;Database=...;User id=...;password=... # database connection string
REM set GoogleClientId=...  # for google oauth, get client id from google console
REM set GoogleSecret=... # for google oauth, get secret from google console

REM set COINMARKETCAPToken=.... # coinmarketcap token that you will need for crypto prices

cd src\web
dotnet watch run