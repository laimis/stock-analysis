$imageName = "stock-site"
$revision = [System.DateTime]::UtcNow.ToString("ryyyy.MM.ddbHH.mm.ss")
$registry = "184947213509.dkr.ecr.us-east-1.amazonaws.com"
$repository = "stock-site"

Invoke-Expression "docker build -t $imageName ."

Invoke-Expression "docker tag $($imageName):latest $($imageName):$revision"

Invoke-Expression "docker tag $($imageName):$revision $registry/$($repository):$imageName-$revision"

$e = aws ecr get-login --profile personal --no-include-email

Invoke-expression $e

Invoke-Expression "docker push $registry/$($repository):$imageName-$revision"