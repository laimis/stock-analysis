param($message)

if ([System.String]::IsNullOrEmpty($message))
{
    write-host "Provide message"
    exit
}

Invoke-Expression 'git push'

$v = Invoke-Expression 'git describe --tags --abbrev=0'

write-host $v

$version = new-object System.Version($v.Substring(1))

$newVersion = new-object System.Version($version.Major, $version.Minor, ($version.Build + 1))

Set-Location .\src\web\ClientApp

Invoke-Expression "npm version $($newVersion)"

Invoke-Expression "git add ."
Invoke-Expression "git commit -m '$message'"
Invoke-Expression "git push"

$cmd = "git tag -a v$($newVersion) -m '$message'"
Invoke-Expression $cmd
Invoke-Expression "git push --tags"

Set-Location ..\..\..