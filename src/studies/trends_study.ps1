$studyDirectory = "d:\\studies\\trends"

$ErrorActionPreference = "Stop"

# create timestamp based on date and time
$timestampPortion = [System.DateTime]::Now.ToString("yyyyMMdd_hhmmss")

write-host "heyo!"

# we need to get the output filename from command line
if ($args.Length -ne 4)
{
    Write-Host "Usage: .\trends_study.ps1 <output filename> <ticker> <years> <trendtype>"
    Write-Host "Example: .\trends_study.ps1 trends.csv AAPL 5 Ema20OverSma50"
    Write-Host "Example: .\trends_study.ps1 trends.csv AAPL 5 Sma50OverSma200"
    exit 1
}

$outputFilename = $args[0]
$ticker = $args[1]
$years = $args[2]
$trendType = $args[3]

& .\dev_secret.bat -o "$studyDirectory\$outputFilename" -t $ticker -y $years -tt $trendType
