[CmdletBinding()]
param(
    [string]$BaseUrl = "http://localhost:4602"
)

$ErrorActionPreference = "Stop"
$baseUrl = $BaseUrl.TrimEnd("/")
$runId = Get-Date -Format "yyyyMMddHHmmss"
$productId = "DEMO-$runId"
$customerId = "CUST-$runId"

function Write-Section {
    param([string]$Title)

    Write-Host ""
    Write-Host "=== $Title ===" -ForegroundColor Magenta
}

function Invoke-Api {
    param(
        [string]$Method,
        [string]$Path,
        [string]$Desc,
        [object]$Body = $null
    )

    $uri = "$baseUrl$Path"
    Write-Host "[$Method] $Desc" -ForegroundColor Yellow
    Write-Host "  $uri" -ForegroundColor DarkGray

    $params = @{
        Uri    = $uri
        Method = $Method
    }

    if ($null -ne $Body) {
        $params.Body = $Body | ConvertTo-Json -Depth 10
        $params.ContentType = "application/json"
    }

    $response = Invoke-RestMethod @params
    $response | ConvertTo-Json -Depth 10
    Write-Host ""
    return $response
}

function Assert-Success {
    param(
        [object]$Response,
        [string]$Operation
    )

    if ($null -eq $Response -or $Response.code -ne 200) {
        throw "$Operation failed."
    }
}

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  DotNetModulith API Smoke Test" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "BaseUrl: $baseUrl" -ForegroundColor Gray
Write-Host "RunId  : $runId" -ForegroundColor Gray

try {
    Write-Section "Health"
    $alive = Invoke-Api -Method "GET" -Path "/alive" -Desc "Liveness probe"
    $startup = Invoke-Api -Method "GET" -Path "/startup" -Desc "Startup probe"
    $ready = Invoke-Api -Method "GET" -Path "/ready" -Desc "Readiness probe"
    $health = Invoke-Api -Method "GET" -Path "/health" -Desc "Health alias"

    Assert-Success -Response $alive -Operation "Alive probe"
    Assert-Success -Response $startup -Operation "Startup probe"
    Assert-Success -Response $ready -Operation "Ready probe"
    Assert-Success -Response $health -Operation "Health probe"

    Write-Section "Modules"
    $modules = Invoke-Api -Method "GET" -Path "/api/modules" -Desc "List modules"
    $graph = Invoke-Api -Method "GET" -Path "/api/modules/graph" -Desc "Module dependency graph"
    $verify = Invoke-Api -Method "GET" -Path "/api/modules/verify" -Desc "Module boundary verification"

    Assert-Success -Response $modules -Operation "Get modules"
    Assert-Success -Response $graph -Operation "Get module graph"
    Assert-Success -Response $verify -Operation "Verify modules"

    Write-Section "Inventory"
    $stockBody = @{
        productId = $productId
        productName = "Demo Product $runId"
        initialQuantity = 10
    }

    $createStock = Invoke-Api -Method "POST" -Path "/api/inventory/stocks" -Desc "Create stock" -Body $stockBody
    $getStock = Invoke-Api -Method "GET" -Path "/api/inventory/stocks/$productId" -Desc "Query stock"
    $replenish = Invoke-Api -Method "POST" -Path "/api/inventory/stocks/$productId/replenish" -Desc "Replenish stock" -Body @{ quantity = 5 }

    Assert-Success -Response $createStock -Operation "Create stock"
    Assert-Success -Response $getStock -Operation "Get stock"
    Assert-Success -Response $replenish -Operation "Replenish stock"

    Write-Section "Orders"
    $orderBody = @{
        customerId = $customerId
        lines = @(
            @{
                productId = $productId
                productName = "Demo Product $runId"
                quantity = 2
                unitPrice = 99.00
            }
        )
    }

    $createOrder = Invoke-Api -Method "POST" -Path "/api/orders" -Desc "Create order" -Body $orderBody
    Assert-Success -Response $createOrder -Operation "Create order"

    $orderId = $createOrder.data.orderId
    if ([string]::IsNullOrWhiteSpace($orderId)) {
        throw "Create order succeeded but orderId is missing."
    }

    $getOrder = Invoke-Api -Method "GET" -Path "/api/orders/$orderId" -Desc "Query order"
    Assert-Success -Response $getOrder -Operation "Get order"

    Write-Host "============================================" -ForegroundColor Cyan
    Write-Host "  Smoke test completed successfully." -ForegroundColor Green
    Write-Host "============================================" -ForegroundColor Cyan
    Write-Host "Scalar UI    : $baseUrl/scalar/v1" -ForegroundColor White
    Write-Host "OpenAPI JSON : $baseUrl/openapi/v1.json" -ForegroundColor White
    Write-Host "CAP Dashboard: $baseUrl/cap-dashboard" -ForegroundColor White
}
catch {
    Write-Host ""
    Write-Host "Smoke test failed." -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}
