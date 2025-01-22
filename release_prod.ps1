param($message)

function Speak-Message ($message) {
    $voice = New-Object -ComObject Sapi.spvoice
    
    # get voices, use one where Id contains ZIRA
    $voices = $voice.GetVoices()
    $voice.Voice = $voices | Where-Object { $_.Id.Contains("ZIRA") }
    $voice.rate = 0
    $voice.speak($message)
}

function Exit-With-Error ($message) {
    write-host $message
    Speak-Message $message
    exit 1
}

function Git-Checkout-Merge($message) {
    $v = Invoke-Expression 'git describe --tags --abbrev=0'
    write-host $v
    $version = new-object System.Version($v.Substring(1))
    $newVersion = new-object System.Version($version.Major, $version.Minor, ($version.Build + 1))

    $cmd = "git checkout prod"
    Invoke-Expression $cmd
    
    $cmd = "git pull"
    Invoke-Expression $cmd
    
    $cmd = "git merge main -m '$message'"
    Invoke-Expression $cmd
    
    $cmd = "git tag -a v$($newVersion) -m '$message'"
    Invoke-Expression $cmd
    
    Invoke-Expression "git push --tags"
    $cmd = "git push"
    Invoke-Expression $cmd
    
    $cmd = "git checkout main"
    Invoke-Expression $cmd
}


function Ensure-Angular-Builds() {
    # ensure that the project can build by invoking npm run build -- --configuration production
    push-location "src/frontend"
    invoke-expression "npm run build -- --configuration production"
    $exitCode = $LASTEXITCODE
    pop-location
    if ($exitCode -ne 0) {
        Exit-With-Error "Angular failed"
    }
}

function Ensure-Angular-Lints() {
    # ensure that the web project lint is clean
    push-location "src/frontend"
    invoke-expression "ng lint"
    $exitCode = $LASTEXITCODE
    Pop-Location
    if ($exitCode -ne 0) {
        Exit-With-Error "Lint failed"
    }
}

function Get-Release-Comment() {
    if ([System.String]::IsNullOrEmpty($message))
    {
        $lastCommit = Get-LastCommitMessage
        write-host "Message is missing, would you like to use the last commit message: $lastCommit"
        $response = Read-Host "y/n"
        if ($response -eq "y")
        {
            $message = $lastCommit
        }
        else
        {
            Exit-With-Error "Message is missing, exiting"
        }
    }

    # short hand to use the last commit message, don't ask why 'y' is used
    if ($message -eq "y")
    {
        $message = Get-LastCommitMessage
    }

    # ensure that $message has "'" escaped
    $message = $message -replace "'", "''"

    return $message
}

function Ensure-Git-Clean() {
    # check if there are any git changes, and if there are, report them and exit
    $gitStatus = git status --porcelain
    if ($null -ne $gitStatus) {

        # store message as multiline string
        $message = "
    There are uncommitted changes in git.
    Git status:
    $gitStatus
    "

        Exit-With-Error $message
    }
}

function Ensure-Tests-Pass() {
    #ensure that tests pass
    Invoke-Expression '.\test.bat'
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0) {
        Exit-With-Error "Tests failed"
    }

    #ensure database tests pass
    Invoke-Expression '.\test_database_secret.bat'
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0) {
        Exit-With-Error "Database tests failed"
    }
}

Ensure-Git-Clean

$message = Get-Release-Comment

Ensure-Tests-Pass

Ensure-Angular-Lints

Ensure-Angular-Builds

Git-Checkout-Merge $message

Speak-Message "Release Complete"
