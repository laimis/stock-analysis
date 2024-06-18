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
    param (
        [string]$filter,
        [string]$filterDirection
    )

    $filterFile = "filter.txt"
    $filterDirectionFile = "filter_direction.txt"
    $resultsDirectoryFile = "results_directory.txt"
    $studyDirectoryFile = "study_directory.txt"
    
    #ensure results directory exists
    if (-not (Test-Path $resultsDirectory)) {
        New-Item -ItemType Directory -Path $resultsDirectory
    }
    
    #ensure results subdirectory with format filter_filterDirection exists
    $resultsSubDirectory = "$resultsDirectory\\$($filter)_$($filterDirection)"
    if (-not (Test-Path $resultsSubDirectory)) {
        New-Item -ItemType Directory -Path $resultsSubDirectory
    }

    # write filter to file
    $filter | Out-File -FilePath $filterFile -Force -NoNewline

    # write filter direction to file
    $filterDirection | Out-File -FilePath $filterDirectionFile -Force -NoNewline
    
    # write results directory to file
    $resultsDirectory | Out-File -FilePath $resultsDirectoryFile -Force -NoNewline
    
    # write study directory to file
    $studyDirectory | Out-File -FilePath $studyDirectoryFile -Force -NoNewline
}

function ClearParameterFiles() {
    $filterFile = "filter.txt"
    $filterDirectionFile = "filter_direction.txt"
    $resultsDirectoryFile = "results_directory.txt"
    $studyDirectoryFile = "study_directory.txt"

    Remove-Item -Path $filterFile -Force
    Remove-Item -Path $filterDirectionFile -Force
    Remove-Item -Path $resultsDirectoryFile -Force
    Remove-Item -Path $studyDirectoryFile -Force
}

# write a function that will call jupyter notebook to execute the notebook, each time creating two files before calling jupyter (accept as params):
# 1. filter.txt where you specify NoFilter, MyFilter, SpyShortTermFilter, and SpyLongTermFilter
# 2. for each of those, specify filter_direction.txt, with All for NoFilter, Down and Up for the rest
function ExecuteNotebook() {
    param (
        [string]$filter,
        [string]$filterDirection
    )
    
    SeedParameterFiles -filter $filter -filterDirection $filterDirection
    
    Write-Host "Executing notebook with filter $filter and filter direction $filterDirection, output to $resultsDirectory"
    
    # execute notebook
    jupyter nbconvert --ExecutePreprocessor.timeout=None --InteractiveShell.iopub_timeout=0 --execute --to html --no-input .\breakout_notebook.ipynb

    # if last exit code is not 0, exit
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    # clear output
    jupyter nbconvert --clear-output .\breakout_notebook.ipynb

    # move file to secret file
    $outputName = "$($resultsDirectory)\notebook_$($filter)_$($filterDirection).html"
    Write-Host "Moving breakout_notebook.html to $outputName"
    Move-Item -Path breakout_notebook.html -Destination $outputName -Force

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
    SeedParameterFiles -filter "NoFilter" -filterDirection "All"
}

if ($args -contains "--clear") {
    ClearParameterFiles
}

if ($args -contains "--interactive") {
    InteractiveRun
}

if ($args -contains "--execute") {
    ExecuteNotebook -filter "NoFilter" -filterDirection "All"
    ExecuteNotebook -filter "MyCycle" -filterDirection "Down"
    ExecuteNotebook -filter "MyCycle" -filterDirection "Up"
    ExecuteNotebook -filter "SpyShortTermCycle" -filterDirection "Down"
    ExecuteNotebook -filter "SpyShortTermCycle" -filterDirection "Up"
    ExecuteNotebook -filter "SpyLongTermCycle" -filterDirection "Down"
    ExecuteNotebook -filter "SpyLongTermCycle" -filterDirection "Up"
}
