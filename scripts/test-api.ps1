[CmdletBinding()]
param(
    [string]$BaseUrl = "http://localhost:5280",
    [string]$OutputPath = ""
)

$ErrorActionPreference = "Stop"
$baseUrl = $BaseUrl.TrimEnd("/")
$runId = Get-Date -Format "yyyyMMddHHmmss"

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $PSScriptRoot "captcha-$runId.svg"
}

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
Write-Host "  DotNetModulith Captcha API Test" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "BaseUrl   : $baseUrl" -ForegroundColor Gray
Write-Host "RunId     : $runId" -ForegroundColor Gray
Write-Host "OutputSvg : $OutputPath" -ForegroundColor Gray

try {
    Write-Section "Captcha"
    $captcha = Invoke-Api -Method "GET" -Path "/api/auth/captcha" -Desc "Get captcha"
    Assert-Success -Response $captcha -Operation "Get captcha"

    $captchaId = $captcha.data.captchaId
    $svgContent = $captcha.data.svgContent

    if ([string]::IsNullOrWhiteSpace($captchaId)) {
        throw "Get captcha succeeded but captchaId is missing."
    }

    if ([string]::IsNullOrWhiteSpace($svgContent)) {
        throw "Get captcha succeeded but svgContent is missing."
    }

    $svgContent | Set-Content -Path $OutputPath -Encoding utf8

    Write-Host "============================================" -ForegroundColor Cyan
    Write-Host "  Captcha API test completed successfully." -ForegroundColor Green
    Write-Host "============================================" -ForegroundColor Cyan
    Write-Host "CaptchaId : $captchaId" -ForegroundColor White
    Write-Host "SVG File  : $OutputPath" -ForegroundColor White
    Write-Host "CaptchaApi: $baseUrl/api/auth/captcha" -ForegroundColor White
}
catch {
    Write-Host ""
    Write-Host "Captcha API test failed." -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}
