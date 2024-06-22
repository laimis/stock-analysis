$ErrorActionPreference = "Stop"

$studyDirectory = "d:\\studies\\breakout_study_2024"

# create timestamp based on date and time
$timestampPortion = [System.DateTime]::Now.ToString("yyyyMMdd_HHmmss")

$resultsDirectory = "$($studyDirectory)\\results_$($timestampPortion)"


write-host "I continue to execute happily!"

# write a function that fetches the data from the API
function FetchData() {
    & .\dev_secret.bat -i "https://localhost:5001/screeners/28/results/export" -o "$studyDirectory\signals_newhighs.csv" 
    & .\dev_secret.bat -i "https://localhost:5001/screeners/29/results/export" -o "$studyDirectory\signals_topgainers.csv"
}

function GenerateSignals() {
    $outputFile = "$studyDirectory\\signals.csv"

    # check if output file exists, and if it does, remove it
    if (Test-Path $outputFile) {
        Remove-Item -Path $outputFile -ErrorAction SilentlyContinue
    }
    
    & .\dev_secret.bat -d $studyDirectory -o $outputFile
}

function InteractiveRun() {
    & .\dev_secret.bat -d $studyDirectory --interactive
}

function SeedParameterFiles() {
    $resultsDirectoryFile = "results_directory.txt"
    $studyDirectoryFile = "study_directory.txt"
    
    #ensure results directory exists
    if (-not (Test-Path $resultsDirectory)) {
        New-Item -ItemType Directory -Path $resultsDirectory
    }
    
    #ensure results subdirectory exists
    $resultsSubDirectory = "$resultsDirectory\\outcomes"
    if (-not (Test-Path $resultsSubDirectory)) {
        New-Item -ItemType Directory -Path $resultsSubDirectory
    }

    # write results directory to file
    $resultsDirectory | Out-File -FilePath $resultsDirectoryFile -Force -NoNewline
    
    # write study directory to file
    $studyDirectory | Out-File -FilePath $studyDirectoryFile -Force -NoNewline
}

function ClearParameterFiles() {
    $resultsDirectoryFile = "results_directory.txt"
    $studyDirectoryFile = "study_directory.txt"

    Remove-Item -Path $resultsDirectoryFile -Force
    Remove-Item -Path $studyDirectoryFile -Force
}

function ExecuteNotebook() {
    
    SeedParameterFiles
    
    Write-Host "Executing notebook, output to $resultsDirectory"
    
    # execute notebook
    jupyter nbconvert --ExecutePreprocessor.timeout=None --InteractiveShell.iopub_timeout=0 --execute --to html --no-input .\breakout_notebook.ipynb

    # if last exit code is not 0, exit
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    # clear output
    jupyter nbconvert --clear-output .\breakout_notebook.ipynb

    # move file to secret file
    $outputName = "$($resultsDirectory)\notebook.html"
    Write-Host "Moving breakout_notebook.html to $outputName"
    Move-Item -Path breakout_notebook.html -Destination $outputName -Force
    
    start $outputName

    ClearParameterFiles
}

# call fetch data funclearction if command line param --fetch is passed
if ($args -contains "--fetch") {
    FetchData
}

if ($args -contains "--generate") {
    GenerateSignals
}

if ($args -contains "--seed") {
    SeedParameterFiles
}

if ($args -contains "--clear") {
    ClearParameterFiles
}

if ($args -contains "--interactive") {
    InteractiveRun
}

if ($args -contains "--execute") {
    ExecuteNotebook
}
