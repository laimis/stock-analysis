# store current directory
$scriptPath = $MyInvocation.MyCommand.Path | Split-Path

# change directory to src/web/ClientApp
Set-Location src/frontend

# get list of packages that need updating
$updates = ng update | Where-Object { $_.Contains("ng update")} 

# if there are no updates available, tell that and exit
if ($updates.Count -eq 0) {
    Write-Host "No updates available"
    Set-Location $scriptPath
    exit
}

Write-Host "There are updates available:"

# use voice to tell that there are updates available
$voice = New-Object -ComObject Sapi.spvoice
$voice.rate = 0
$voice.speak("There are updates available")

$updates | ForEach-Object { Write-Host $_ }
Write-Host "Apply them? (y/n)"
$answer = Read-Host

# if answer is not y, exit
if ($answer -ne "y") {
    Set-Location $scriptPath
    exit
}

# check if there are any git changes, and if there are, report them and exit
$gitStatus = git status --porcelain
if ($null -ne $gitStatus) {
    Write-Host "There are uncommitted changes in git. Please commit or stash them and try again."
    Write-Host "Git status:"
    Write-Host $gitStatus
    Set-Location $scriptPath

    # exit with error code
    exit 1
}

# loop through each package and update
$updates | ForEach-Object {
    $package = [regex]::Match($_, "@[^\s]+").Value
    Write-Host "Applying update: $package"
    ng update $package

    # check if the above commit was successful, if not, exit
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Update failed, exiting"
        Set-Location $scriptPath
        exit 1
    }

    git add .
    git commit -m "Angular updates: $package"
    git push
}


# change directory back to original
Set-Location $scriptPath
