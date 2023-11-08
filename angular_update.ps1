# store current directory
$scriptPath = $MyInvocation.MyCommand.Path | Split-Path

# change directory to src/web/ClientApp
Set-Location src/web/ClientApp

# get list of packages that need updating
$updates = ng update | Where-Object { $_.Contains("@angular")} 

# if there are no updates available, tell that and exit
if ($updates.Count -eq 0) {
    Write-Host "No updates available"
    exit
}

# ask if to continue
Write-Host "The following updates are available:"
$updates | ForEach-Object { Write-Host $_ }
Write-Host "Continue? (y/n)"
$answer = Read-Host

# if answer is not y, exit
if ($answer -ne "y") {
    exit
}

# check if there are any git changes, and if there are, report them and exit
$gitStatus = git status --porcelain
if ($null -ne $gitStatus) {
    Write-Host "There are uncommitted changes in git. Please commit or stash them and try again."
    Write-Host "Git status:"
    Write-Host $gitStatus

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
        exit 1
    }

    git add .
    git commit -m "Angular updates: $package"
}


# change directory back to original
Set-Location $scriptPath