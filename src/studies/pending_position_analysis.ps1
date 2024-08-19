# get a temporary file path

$TempFile = [System.IO.Path]::GetTempFileName()

jupyter nbconvert --to html --no-input --execute .\pending_position_analysis.ipynb --output $TempFile

# clear output
jupyter nbconvert --clear-output .\pending_position_analysis.ipynb

#prompt to erase the temporary file when ready
Read-Host -Prompt "Press Enter to delete the temporary file"

# Remove the temporary file
Remove-Item $TempFile
