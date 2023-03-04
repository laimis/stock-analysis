param($message)

if ([System.String]::IsNullOrEmpty($message))
{
    write-host "Provide message"
    exit
}

# ensure that the project can build by invoking npm run build:production
# in src/web/ClientApp directory

push-location "src/web/ClientApp"
invoke-expression "npm run build:production"
$exitCode = $LASTEXITCODE
pop-location
if ($exitCode -ne 0) {
    write-host "Release prep failed"
    exit
}

# make sure garbage collection is not in progress
$garbageCollection = $true
while ($garbageCollection) {
    $collectionJson = invoke-expression 'doctl registry garbage-collection get-active --output json'
    
    $gcCollections = ConvertFrom-Json ([System.String]::Join("", $collectionJson))

    if ($gcCollections.Count -eq 0 -or $gcCollections.errors -ne $null)
    {
        $garbageCollection = $false
    }
    else
    {
        $statusDesc = $gcCollections[0].status
        write-host "Garbage collection is in progress: $statusDesc ..."
        Start-Sleep -Seconds 30
    }
}

Invoke-Expression 'git push'

$v = Invoke-Expression 'git describe --tags --abbrev=0'

write-host $v

$version = new-object System.Version($v.Substring(1))

$newVersion = new-object System.Version($version.Major, $version.Minor, ($version.Build + 1))

$cmd = "git tag -a v$($newVersion) -m '$message'"
Invoke-Expression $cmd
Invoke-Expression "git push --tags"

# clean up garbage
$manifestsJson = invoke-expression 'doctl registry repository list-manifests web --output json'

$manifests = ConvertFrom-Json ([System.String]::Join("", $manifestsJson))

foreach($manifest in $manifests)
{
    if ($manifest.tags.Length -ne 0)
    {
        write-host "no manifests found"
        continue
    }

    write-host "Deleting manifest $($manifest.digest)"
    Invoke-Expression "doctl registry repository delete-manifest web $($manifest.digest) -f"
}