
dropdb -U stockanalysis stockanalysis
createdb -U stockanalysis stockanalysis
pg_dump -U stockanalysis -h HOST -p PORT -d PRODDB -F c -v -f stockanalysis.dump
pg_restore -U stockanalysis -d stockanalysis -v stockanalysis.dump

REM print the size of the database
psql -U stockanalysis -c "SELECT pg_size_pretty(pg_database_size('stockanalysis'))"

REM print the size of the dump file
DIR stockanalysis.dump

erase stockanalysis.dump
