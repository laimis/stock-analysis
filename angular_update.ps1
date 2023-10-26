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

# loop through each package and update
$updates | ForEach-Object {
    $package = [regex]::Match($_, "@[^\s]+").Value
    Write-Host "Applying update: $package"
    ng update $package
    git add .
    git commit -m "Angular updates: $package"
}


# change directory back to original
Set-Location $scriptPath