@REM set outputFile=signals_transformed.csv
@REM
@REM REM first clear transformed file to have a clean run
@REM erase /Q %outputFile%
@REM
@REM cmd /c .\dev_secret.bat -i "https://localhost:5001/screeners/28/results/export" -o signals_newhighs.csv
@REM cmd /c .\dev_secret.bat -i "https://localhost:5001/screeners/29/results/export" -o signals_topgainers.csv
@REM cmd /c .\dev_secret.bat -i "https://localhost:5001/screeners/30/results/export" -o signals_toplosers.csv
@REM cmd /c .\dev_secret.bat -i "https://localhost:5001/screeners/31/results/export" -o signals_newlows.csv
@REM
@REM cmd /c .\dev_secret.bat -pt -f "signals_newhighs.csv" -o %outputFile%
@REM cmd /c .\dev_secret.bat -pt -f "signals_topgainers.csv" -o %outputFile%
@REM cmd /c .\dev_secret.bat -pt -f "signals_toplosers.csv" -o %outputFile%
@REM cmd /c .\dev_secret.bat -pt -f "signals_newlows.csv" -o %outputFile%

jupyter nbconvert --execute --to html --no-input .\notebook.ipynb

@REM if last exit code is not 0, exit
if not %errorlevel% == 0 exit /b %errorlevel%

jupyter nbconvert --clear-output .\notebook.ipynb

move notebook.html notebook_secret.html
start notebook_secret.html
