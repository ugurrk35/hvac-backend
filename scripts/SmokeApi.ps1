param(
    [Parameter(Mandatory = $true)]
    [string]$BaseUrl
)

$ErrorActionPreference = 'Stop'
$base = $BaseUrl.TrimEnd('/')

function Assert-DataResponse([string]$Path) {
    $response = Invoke-RestMethod -Uri "$base$Path" -Method Get
    if ($null -eq $response.success -or $null -eq $response.data) {
        throw "Unexpected response contract for $Path"
    }
    if (-not $response.success) {
        throw "Endpoint returned an unsuccessful response for $Path"
    }
}

$health = Invoke-WebRequest -UseBasicParsing -Uri "$base/health"
if ($health.StatusCode -ne 200 -or $health.Content -ne 'Healthy') {
    throw 'Health endpoint did not return the expected response.'
}

Assert-DataResponse '/api/categories/active'
Assert-DataResponse '/api/productattribute'

Write-Output 'API smoke tests passed.'
