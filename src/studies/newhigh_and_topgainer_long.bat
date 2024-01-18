set outputFile=signals_newhighs_and_topgainer_pricetransformed.csv

REM first clear transformed file to have a clean run
erase /Q %outputFile%

cmd /c .\dev_secret.bat -i "https://localhost:5001/screeners/28/results/export" -o signals_newhighs.csv
cmd /c .\dev_secret.bat -i "https://localhost:5001/screeners/29/results/export" -o signals_topgainers.csv
cmd /c .\dev_secret.bat -pt -f "signals_newhighs.csv" -o %outputFile%
cmd /c .\dev_secret.bat -pt -f "signals_topgainers.csv" -o %outputFile%

start jupyter lab notebook.ipynb