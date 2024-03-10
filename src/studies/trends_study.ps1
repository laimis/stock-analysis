$ErrorActionPreference = "Stop"

$studyDirectory = "d:\\studies\\trends"

# create timestamp based on date and time
$timestampPortion = [System.DateTime]::Now.ToString("yyyyMMdd_hhmmss")

write-host "heyo!"

# we need to get the output filename from command line
if ($args.Length -ne 3)
{
    Write-Error "Usage: .\trends_study.ps1 <output filename> <ticker> <years>"
    exit 1
}

$outputFilename = $args[0]
$ticker = $args[1]
$years = $args[2]

& .\dev_secret.bat -o "$studyDirectory\$outputFilename" -t $ticker -y $years
