set outputFile=signals_transformed.csv

REM first clear transformed file to have a clean run
erase /Q %outputFile%

cmd /c .\dev_secret.bat -i "https://localhost:5001/screeners/28/results/export" -o signals_newhighs.csv
cmd /c .\dev_secret.bat -i "https://localhost:5001/screeners/29/results/export" -o signals_topgainers.csv
cmd /c .\dev_secret.bat -i "https://localhost:5001/screeners/30/results/export" -o signals_toplosers.csv
cmd /c .\dev_secret.bat -i "https://localhost:5001/screeners/31/results/export" -o signals_newlows.csv

cmd /c .\dev_secret.bat -pt -f "signals_newhighs.csv" -o %outputFile%
cmd /c .\dev_secret.bat -pt -f "signals_topgainers.csv" -o %outputFile%
cmd /c .\dev_secret.bat -pt -f "signals_toplosers.csv" -o %outputFile%
cmd /c .\dev_secret.bat -pt -f "signals_newlows.csv" -o %outputFile%

start jupyter lab notebook.ipynb