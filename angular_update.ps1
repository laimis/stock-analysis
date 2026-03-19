# store current directory
$scriptPath = $MyInvocation.MyCommand.Path | Split-Path

function Assert-NoUncommittedChanges {
    $gitStatus = git status --porcelain
    if ($null -ne $gitStatus) {
        Write-Host "There are uncommitted changes in git. Please commit or stash them and try again."
        Write-Host "Git status:"
        Write-Host $gitStatus
        Set-Location $scriptPath
        exit 1
    }
}

# change directory to src/frontend
Set-Location src/frontend

# --- Angular updates ---

$updates = ng update | Where-Object { $_ -match "ng update \S+" }

if ($updates.Count -eq 0) {
    Write-Host "No Angular updates available"
} else {
    Write-Host "There are Angular updates available:"

    # use voice to tell that there are updates available
    $voice = New-Object -ComObject Sapi.spvoice
    $voice.rate = 0
    $voice.speak("There are updates available")

    $updates | ForEach-Object { Write-Host $_ }
    Write-Host "Apply them? (y/n)"
    $answer = Read-Host

    if ($answer -eq "y") {
        Assert-NoUncommittedChanges

        $updates | ForEach-Object {
            $command = [regex]::Match($_, "(ng update \S+)").Groups[1].Value
            Write-Host "Applying update: $command"
            Invoke-Expression $command

            if ($LASTEXITCODE -ne 0) {
                Write-Host "Update failed, exiting"
                Set-Location $scriptPath
                exit 1
            }

            $package = [regex]::Match($command, "ng update (.+)").Groups[1].Value
            git add .
            git commit -m "Angular updates: $package"
            git push
        }
    }
}

# --- npm audit ---

Write-Host ""
Write-Host "Running npm audit..."
npm audit

if ($LASTEXITCODE -eq 0) {
    Write-Host "No vulnerabilities found"
} else {
    Write-Host "Run npm audit fix? (y/n)"
    $auditAnswer = Read-Host

    if ($auditAnswer -eq "y") {
        Assert-NoUncommittedChanges

        npm audit fix

        if ($LASTEXITCODE -ne 0) {
            Write-Host "npm audit fix failed, exiting"
            Set-Location $scriptPath
            exit 1
        }

        git add .
        git commit -m "npm audit fix"
        git push
    }
}

# change directory back to original
Set-Location $scriptPath
