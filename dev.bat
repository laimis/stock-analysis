REM fully functioning app needs teh following environment variables set:

REM set storage=[postgres|memory]
REM set DB_CNN=Server=...;Database=...;User id=...;password=... # database connection string
REM set GoogleClientId=...  # for google oauth, get client id from google console
REM set GoogleSecret=... # for google oauth, get secret from google console
REM set ADMINEmail=... # email address for admin user, when a user with this address logs in, they have more privileges

REM set COINMARKETCAPToken=.... # coinmarketcap token that you will need for crypto prices

cd src\web
dotnet watch run