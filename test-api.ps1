$baseUrl = "http://localhost:4602"

function Call-Api {
    param([string]$Method, [string]$Path, [object]$Body = $null, [string]$Desc)
    Write-Host "[$Method] $Desc" -ForegroundColor Yellow
    try {
        $params = @{ Uri = "$baseUrl$Path"; Method = $Method }
        if ($Body) { $params.Body = ($Body | ConvertTo-Json -Depth 5 -Compress); $params.ContentType = "application/json" }
        $resp = Invoke-RestMethod @params
        $resp | ConvertTo-Json -Depth 10
    } catch {
        Write-Host "  Error: $($_.Exception.Message)" -ForegroundColor Red
    }
    Write-Host ""
    Start-Sleep -Milliseconds 300
}

function Check-Traces {
    param([int]$ExpectedMin = 1)
    Write-Host "Querying OpenObserve for distributed traces..." -ForegroundColor Yellow
    try {
        $creds = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes("admin@modulith.local:Modulith@2026"))
        $headers = @{ Authorization = "Basic $creds" }
        $now = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds() * 1000
        $hourAgo = $now - 3600000000
        $body = "{`"query`":{`"sql`":`"SELECT trace_id, operation_name, service_name, duration FROM dotnet_modulith WHERE operation_name LIKE '%Order%' OR operation_name LIKE '%Stock%' OR operation_name LIKE '%Payment%' OR operation_name LIKE '%Handle%' OR operation_name LIKE '%Publish%' ORDER BY _timestamp DESC`",`"start_time`":$hourAgo,`"end_time`":$now},`"size`":20}"
        $resp = Invoke-RestMethod -Uri "http://localhost:5080/api/default/_search?type=traces" -Method POST -Headers $headers -ContentType "application/json" -Body $body
        $count = if ($resp.hits) { $resp.hits.Count } else { 0 }
        if ($count -ge $ExpectedMin) {
            Write-Host "  Found $count trace spans (expected >= $ExpectedMin)" -ForegroundColor Green
            Write-Host ""
            Write-Host "  Key distributed trace spans:" -ForegroundColor Cyan
            $resp.hits | ForEach-Object {
                $durMs = [math]::Round($_.duration / 1000000, 2)
                Write-Host "    $($_.service_name) | $($_.operation_name) | ${durMs}ms" -ForegroundColor White
            }
        } else {
            Write-Host "  Found $count trace spans (expected >= $ExpectedMin) - traces may still be ingesting" -ForegroundColor DarkYellow
        }
    } catch {
        Write-Host "  Could not query OpenObserve: $($_.Exception.Message)" -ForegroundColor DarkYellow
        Write-Host "  Make sure OpenObserve is running on http://localhost:5080" -ForegroundColor DarkGray
    }
    Write-Host ""
}

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  DotNetModulith API Test Script" -ForegroundColor Cyan
Write-Host "  Full Event Flow + Distributed Tracing" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "=== Step 1: Create Stock Records ===" -ForegroundColor Magenta
Write-Host ""

Call-Api -Method "POST" -Path "/api/inventory/stocks" -Body @{ productId="PROD-001"; productName="MechKeyboard"; initialQuantity=100 } -Desc "Create PROD-001 stock (qty: 100)"
Call-Api -Method "POST" -Path "/api/inventory/stocks" -Body @{ productId="PROD-002"; productName="WirelessMouse"; initialQuantity=200 } -Desc "Create PROD-002 stock (qty: 200)"
Call-Api -Method "POST" -Path "/api/inventory/stocks" -Body @{ productId="PROD-003"; productName="4KMonitor"; initialQuantity=50 } -Desc "Create PROD-003 stock (qty: 50)"

Write-Host "=== Step 2: Query Stocks ===" -ForegroundColor Magenta
Write-Host ""

Call-Api -Method "GET" -Path "/api/inventory/stocks/PROD-001" -Desc "Query PROD-001 stock"
Call-Api -Method "GET" -Path "/api/inventory/stocks/PROD-002" -Desc "Query PROD-002 stock"

Write-Host "=== Step 3: Create Order (Triggers Full Event Flow) ===" -ForegroundColor Magenta
Write-Host ""
Write-Host "Expected distributed trace spans:" -ForegroundColor DarkGray
Write-Host "  1. HTTP POST /api/orders" -ForegroundColor DarkGray
Write-Host "  2. CreateOrderCommand handler" -ForegroundColor DarkGray
Write-Host "  3. Order.Create() -> OrderCreatedDomainEvent" -ForegroundColor DarkGray
Write-Host "  4. PublishIntegrationEvent.OrderCreatedDomainEvent (CAP/RabbitMQ)" -ForegroundColor DarkGray
Write-Host "  5. HandleOrderCreated (Inventory) -> Reserve stock -> StockReservedIntegrationEvent" -ForegroundColor DarkGray
Write-Host "  6. HandleOrderCreated (Payments) -> Process payment -> PaymentCompletedIntegrationEvent" -ForegroundColor DarkGray
Write-Host "  7. HandlePaymentCompleted (Orders) -> Mark order as paid" -ForegroundColor DarkGray
Write-Host "  8. HandleOrderCreated (Notifications) -> Send notification" -ForegroundColor DarkGray
Write-Host ""

$orderBody = @{
    customerId = "CUST-2026-001"
    lines = @(
        @{ productId="PROD-001"; productName="MechKeyboard"; quantity=2; unitPrice=599.00 },
        @{ productId="PROD-002"; productName="WirelessMouse"; quantity=3; unitPrice=199.00 }
    )
}
Call-Api -Method "POST" -Path "/api/orders" -Body $orderBody -Desc "Create order: CUST-2026-001, 2x MechKeyboard + 3x WirelessMouse"

Write-Host "=== Step 4: Wait for async event processing ===" -ForegroundColor Magenta
Write-Host "Waiting 8 seconds for CAP/RabbitMQ event processing..." -ForegroundColor DarkGray
Start-Sleep -Seconds 8

Write-Host "=== Step 5: Verify Stock Reserved ===" -ForegroundColor Magenta
Write-Host ""

Call-Api -Method "GET" -Path "/api/inventory/stocks/PROD-001" -Desc "PROD-001 stock (expected: available=98, reserved=2)"
Call-Api -Method "GET" -Path "/api/inventory/stocks/PROD-002" -Desc "PROD-002 stock (expected: available=197, reserved=3)"

Write-Host "=== Step 6: Create Second Order ===" -ForegroundColor Magenta
Write-Host ""

$orderBody2 = @{
    customerId = "CUST-2026-002"
    lines = @(
        @{ productId="PROD-003"; productName="4KMonitor"; quantity=1; unitPrice=3999.00 }
    )
}
Call-Api -Method "POST" -Path "/api/orders" -Body $orderBody2 -Desc "Create order: CUST-2026-002, 1x 4KMonitor"

Start-Sleep -Seconds 8

Write-Host "=== Step 7: Module Info ===" -ForegroundColor Magenta
Write-Host ""

Call-Api -Method "GET" -Path "/api/modules" -Desc "Module list"
Call-Api -Method "GET" -Path "/api/modules/graph" -Desc "Module dependency graph"
Call-Api -Method "GET" -Path "/api/modules/verify" -Desc "Module boundary verification"

Write-Host "=== Step 8: Health Check ===" -ForegroundColor Magenta
Write-Host ""

Call-Api -Method "GET" -Path "/health" -Desc "Health check"

Write-Host "=== Step 9: Verify Distributed Traces in OpenObserve ===" -ForegroundColor Magenta
Write-Host ""

Check-Traces -ExpectedMin 5

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Test Complete!" -ForegroundColor Cyan
Write-Host ""
Write-Host "  View Distributed Traces in OpenObserve:" -ForegroundColor Green
Write-Host "  http://localhost:5080" -ForegroundColor White
Write-Host "    User: admin@modulith.local" -ForegroundColor Gray
Write-Host "    Pass: Modulith@2026" -ForegroundColor Gray
Write-Host "    Path: Traces -> default -> dotnet_modulith" -ForegroundColor Gray
Write-Host ""
Write-Host "  API Documentation:" -ForegroundColor Green
Write-Host "  https://localhost:4601/scalar/v1" -ForegroundColor White
Write-Host ""
Write-Host "  CAP Dashboard:" -ForegroundColor Green
Write-Host "  https://localhost:4601/cap-dashboard" -ForegroundColor White
Write-Host ""
Write-Host "  RabbitMQ Management:" -ForegroundColor Green
Write-Host "  http://localhost:15672 (guest/guest)" -ForegroundColor White
Write-Host "============================================" -ForegroundColor Cyan
