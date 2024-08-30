# get a temporary file path

$TempFile = [System.IO.Path]::GetTempFileName()

jupyter nbconvert --to html --no-input --execute .\simulated_position_analysis.ipynb --output $TempFile

# clear output
jupyter nbconvert --clear-output .\simulated_position_analysis.ipynb

#prompt to erase the temporary file when ready
Read-Host -Prompt "Press Enter to delete the temporary file"

# Remove the temporary file
Remove-Item $TempFile
