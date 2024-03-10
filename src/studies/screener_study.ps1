$ErrorActionPreference = "Stop"

$studyDirectory = "d:\\studies\\screeners"

# create timestamp based on date and time
$timestampPortion = [System.DateTime]::Now.ToString("yyyyMMdd_hhmmss")

write-host "I continue to execute happily!"

# write a function that fetches the data from the API
function FetchData() {
    & .\dev_secret.bat -i "https://localhost:5001/screeners/28/results/export" -o signals_newhighs.csv 
    & .\dev_secret.bat -i "https://localhost:5001/screeners/29/results/export" -o signals_topgainers.csv
    & .\dev_secret.bat -i "https://localhost:5001/screeners/30/results/export" -o signals_toplosers.csv
    & .\dev_secret.bat -i "https://localhost:5001/screeners/31/results/export" -o signals_newlows.csv
}

function TransformSignals() {
    $outputFile = "$studyDirectory\\signals_transformed.csv"

    # check if output file exists, and if it does, remove it
    if (Test-Path $outputFile) {
        Remove-Item -Path $outputFile -ErrorAction SilentlyContinue
    }
    
    & .\dev_secret.bat -pt -d $studyDirectory -f "$studyDirectory\signals_newhighs.csv" -o $outputFile
    & .\dev_secret.bat -pt -d $studyDirectory -f "$studyDirectory\signals_topgainers.csv" -o $outputFile
    & .\dev_secret.bat -pt -d $studyDirectory -f "$studyDirectory\signals_toplosers.csv" -o $outputFile
    & .\dev_secret.bat -pt -d $studyDirectory -f "$studyDirectory\signals_newlows.csv" -o $outputFile
}

# write a function that will call jupyter notebook to execute the notebook, each time creating two files before calling jupyter (accept as params):
# 1. filter.txt where you specify NoFilter, MyFilter, SpyShortTermFilter, and SpyLongTermFilter
# 2. for each of those, specify filter_direction.txt, with All for NoFilter, Down and Up for the rest
function ExecuteNotebook() {
    param (
        [string]$filter,
        [string]$filterDirection
    )

    $filterFile = "filter.txt"
    $filterDirectionFile = "filter_direction.txt"
    $resultsDirectoryFile = "results_directory.txt"
    $resultsDirectory = "$($studyDirectory)\\results_$($timestampPortion)"

    #ensure results directory exists
    if (-not (Test-Path $resultsDirectory)) {
        New-Item -ItemType Directory -Path $resultsDirectory
    }
    
    #ensure results subdirectory with format filter_filterDirection exists
    $resultsSubDirectory = "$resultsDirectory\\$($filter)_$($filterDirection)"
    if (-not (Test-Path $resultsSubDirectory)) {
        New-Item -ItemType Directory -Path $resultsSubDirectory
    }

    Write-Host "Executing notebook with filter $filter and filter direction $filterDirection, output to $resultsDirectory"

    # write filter to file
    $filter | Out-File -FilePath $filterFile -Force -NoNewline

    # write filter direction to file
    $filterDirection | Out-File -FilePath $filterDirectionFile -Force -NoNewline
    
    # write results directory to file
    $resultsDirectory | Out-File -FilePath $resultsDirectoryFile -Force -NoNewline

    # execute notebook
    jupyter nbconvert --ExecutePreprocessor.timeout=None --InteractiveShell.iopub_timeout=0 --execute --to html --no-input .\notebook.ipynb

    # if last exit code is not 0, exit
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    # clear output
    jupyter nbconvert --clear-output .\notebook.ipynb

    # move file to secret file
    $outputName = "$($resultsDirectory)\notebook_$($filter)_$($filterDirection).html"
    Write-Host "Moving notebook.html to $outputName"
    Move-Item -Path notebook.html -Destination $outputName -Force

    # remove filter files
    Remove-Item -Path $filterFile -ErrorAction SilentlyContinue
    Remove-Item -Path $filterDirectionFile -ErrorAction SilentlyContinue
    Remove-Item -Path $resultsDirectoryFile -ErrorAction SilentlyContinue
}

# call fetch data funclearction if command line param --fetch is passed
if ($args -contains "--fetch") {
    FetchData
}

if ($args -contains "--transform") {
    TransformSignals
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
