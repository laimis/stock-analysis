param($message)

if ([System.String]::IsNullOrEmpty($message))
{
    write-host "Provide message"
    exit
}

$v = iex 'git describe --tags --abbrev=0'

write-host $v

$version = new-object System.Version($v.Substring(1))

$newVersion = new-object System.Version($version.Major, $version.Minor, ($version.Build + 1))

$cmd = "git tag -a v$($newVersion) -m '$message'"

write-host $cmd 

iex $cmd