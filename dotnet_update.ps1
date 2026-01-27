# store current directory
$scriptPath = $MyInvocation.MyCommand.Path | Split-Path

# check if dotnet-outdated-tool is installed
$toolInstalled = dotnet tool list -g | Select-String "dotnet-outdated-tool"
if ($null -eq $toolInstalled) {
    Write-Host "dotnet-outdated-tool is not installed. Installing..."
    dotnet tool install --global dotnet-outdated-tool
}

# get list of outdated packages (capture to check for updates)
$outdatedOutput = dotnet outdated

# check if there are any outdated packages (look for lines with version arrows ->)
$hasUpdates = $outdatedOutput | Where-Object { $_ -match "->" }

if ($null -eq $hasUpdates) {
    Write-Host "No updates available"
    Set-Location $scriptPath
    exit
}

Write-Host "There are updates available:"
Write-Host ""
# Run again without capturing to display properly formatted output
dotnet outdated
Write-Host ""

# use voice to tell that there are updates available
$voice = New-Object -ComObject Sapi.spvoice
$voice.rate = 0
$voice.speak("There are .NET package updates available")

Write-Host "`nApply them? (y/n)"
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

Write-Host "Applying all updates..."
dotnet outdated -u

# check if the above command was successful, if not, exit
if ($LASTEXITCODE -ne 0) {
    Write-Host "Update failed, exiting"
    Set-Location $scriptPath
    exit 1
}

# build to ensure everything still compiles
Write-Host "Building solution to verify updates..."
dotnet build tradewatch.sln

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed after updates. Please review changes."
    Set-Location $scriptPath
    exit 1
}

# commit and push changes
git add .
git commit -m ".NET package updates"
git push

Write-Host "Updates applied and pushed successfully"

# change directory back to original
Set-Location $scriptPath
