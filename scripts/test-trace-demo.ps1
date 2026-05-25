<#
.SYNOPSIS
    链路追踪演示接口测试脚本
.DESCRIPTION
    调用链路追踪演示接口，验证完整链路：API → DB → Cache → Queue → Subscriber
.NOTES
    依赖 test-api.ps1 中的通用 Invoke-Api 函数，需先 dotnet run 启动 API 服务
#>
[CmdletBinding()]
param(
    [string]$BaseUrl = "http://localhost:4602",
    [string]$UserName = "admin",
    [string]$Password = "Admin@123456"
)

$ErrorActionPreference = "Stop"
$baseUrl = $BaseUrl.TrimEnd("/")

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
        [object]$Body = $null,
        [string]$Token = $null
    )

    $uri = "$baseUrl$Path"
    Write-Host "[$Method] $Desc" -ForegroundColor Yellow
    Write-Host "  $uri" -ForegroundColor DarkGray

    $params = @{
        Uri     = $uri
        Method  = $Method
        Headers = @{}
    }

    if ($Token) {
        $params.Headers["Authorization"] = "Bearer $Token"
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

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  DotNetModulith Trace Demo Test" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "BaseUrl: $baseUrl" -ForegroundColor Gray

try {
    # Step 1: Login
    Write-Section "Login"
    Write-Host "Authenticating as $UserName..." -ForegroundColor Gray

    $loginBody = @{
        userName = $UserName
        password = $Password
    }

    $loginResult = Invoke-Api -Method "POST" -Path "/api/auth/login" -Desc "Login" -Body $loginBody

    if ($null -eq $loginResult -or $loginResult.code -ne 200) {
        throw "Login failed."
    }

    $accessToken = $loginResult.data.accessToken
    Write-Host "Token acquired" -ForegroundColor Green

    # Step 2: Run trace demo
    Write-Section "Trace Demo"
    Write-Host "Executing full trace chain..." -ForegroundColor Gray

    $traceResult = Invoke-Api -Method "POST" -Path "/api/trace-demo" -Desc "Run trace demo" -Token $accessToken

    if ($null -eq $traceResult -or $traceResult.code -ne 200) {
        throw "Trace demo failed."
    }

    $demoData = $traceResult.data
    $steps = $demoData.steps

    Write-Host "Demo ID     : $($demoData.demoId)" -ForegroundColor Cyan
    Write-Host "Started At  : $($demoData.startedAt)" -ForegroundColor Gray
    Write-Host "Completed At: $($demoData.completedAt)" -ForegroundColor Gray

    Write-Host ""
    Write-Host "Trace Chain Steps:" -ForegroundColor Cyan
    Write-Host "----------------------------------------------------" -ForegroundColor DarkGray

    $totalDuration = 0
    foreach ($step in $steps) {
        $statusIcon = if ($step.status -eq "success") { "[OK]" } else { "[FAIL]" }
        $duration = $step.durationMs

        Write-Host "  $statusIcon $($step.name)" -ForegroundColor $(if ($step.status -eq "success") { "Green" } else { "Red" })
        Write-Host "      Status : $($step.status)" -ForegroundColor Gray
        Write-Host "      Time   : ${duration}ms" -ForegroundColor Gray

        foreach ($kv in $step.details.PSObject.Properties) {
            Write-Host "      $($kv.Name) : $($kv.Value)" -ForegroundColor DarkGray
        }
        Write-Host ""

        $totalDuration += $duration
    }

    Write-Host "----------------------------------------------------" -ForegroundColor DarkGray
    Write-Host "Total steps : $($steps.Count)" -ForegroundColor White
    Write-Host "Total time  : ${totalDuration}ms" -ForegroundColor White
    Write-Host "Elapsed     : $(([DateTimeOffset]$demoData.completedAt - [DateTimeOffset]$demoData.startedAt).TotalMilliseconds)ms" -ForegroundColor White

    Write-Host ""
    Write-Host "============================================" -ForegroundColor Cyan
    Write-Host "  Trace demo completed successfully." -ForegroundColor Green
    Write-Host "============================================" -ForegroundColor Cyan

    Write-Host ""
    Write-Host "Expected trace chain:" -ForegroundColor Yellow
    Write-Host "  1. API Request    -> TraceDemoController" -ForegroundColor White
    Write-Host "  2. Database Write -> orders.orders table" -ForegroundColor White
    Write-Host "  3. Cache Write    -> FusionCache (L1+L2/Redis)" -ForegroundColor White
    Write-Host "  4. Cache Read     <- FusionCache (L1+L2/Redis)" -ForegroundColor White
    Write-Host "  5. Queue Publish  -> RabbitMQ (CAP)" -ForegroundColor White
    Write-Host "  6. Queue Consume  -> NotificationEventSubscriber" -ForegroundColor White
    Write-Host ""
    Write-Host "OpenTelemetry traces can be viewed in the configured" -ForegroundColor Gray
    Write-Host "observability backend (e.g., Jaeger, Zipkin, or Aspire Dashboard)." -ForegroundColor Gray
    Write-Host ""
    Write-Host "Scalar UI    : $baseUrl/scalar/v1" -ForegroundColor White
    Write-Host "OpenAPI JSON : $baseUrl/openapi/v1.json" -ForegroundColor White
    Write-Host "CAP Dashboard: $baseUrl/cap-dashboard" -ForegroundColor White
}
catch {
    Write-Host ""
    Write-Host "Trace demo test failed." -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red

    if ($_.Exception.Response) {
        try {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $body = $reader.ReadToEnd()
            $reader.Close()
            Write-Host "Response body:" -ForegroundColor DarkRed
            Write-Host ($body | ConvertFrom-Json | ConvertTo-Json -Depth 10) -ForegroundColor DarkRed
        }
        catch {
            # ignore parse errors
        }
    }

    exit 1
}
