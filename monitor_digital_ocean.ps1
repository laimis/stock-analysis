#!/usr/bin/env pwsh
# Nightingale Trading Production Monitor
# Uses doctl (DigitalOcean CLI) to monitor the deployed app

param(
    [Parameter(Position=0)]
    [string]$Command = "menu",

    [string]$DeploymentId,
    [int]$Lines = 100
)

$APP_ID = "40846276-e784-4869-9301-8cb87a50eb44"
$APP_NAME = "ngtrading"
$COMPONENT = "stock-analysis"

function Show-Help {
    Write-Host ""
    Write-Host "Nightingale Trading Production Monitor" -ForegroundColor Cyan
    Write-Host "=======================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Usage: .\monitor_prod.ps1 [command] [options]" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Commands:" -ForegroundColor Green
    Write-Host "  logs          Show latest runtime logs (default)"
    Write-Host "  logs-build    Show latest build logs"
    Write-Host "  logs-deploy   Show latest deploy logs"
    Write-Host "  logs-follow   Follow (tail) runtime logs live"
    Write-Host "  build         Tail build logs if a build is in progress, otherwise report status"
    Write-Host "  deployments   List recent deployments and their state"
    Write-Host "  deployment    Show details of the latest (or specific) deployment"
    Write-Host "  status        Show overall app status"
    Write-Host "  menu          Interactive menu (default when no args given)"
    Write-Host ""
    Write-Host "Options:" -ForegroundColor Green
    Write-Host "  -DeploymentId <id>   Target a specific deployment (used with 'deployment')"
    Write-Host "  -Lines <n>           Number of log lines to fetch (default: 100)"
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor DarkGray
    Write-Host "  .\monitor_prod.ps1 logs"
    Write-Host "  .\monitor_prod.ps1 logs -Lines 200"
    Write-Host "  .\monitor_prod.ps1 logs-follow"
    Write-Host "  .\monitor_prod.ps1 deployments"
    Write-Host "  .\monitor_prod.ps1 deployment"
    Write-Host "  .\monitor_prod.ps1 deployment -DeploymentId <id>"
    Write-Host ""
}

function Get-AppStatus {
    Write-Host ""
    Write-Host "App Status: $APP_NAME" -ForegroundColor Cyan
    Write-Host "─────────────────────────────────────" -ForegroundColor DarkGray
    $app = doctl apps get $APP_ID --output json 2>&1 | ConvertFrom-Json
    $activeDeployId = $app.active_deployment.id
    $phase = $app.active_deployment.phase
    $updatedAt = $app.updated_at

    Write-Host "  App ID:              $APP_ID"
    Write-Host "  Active Deployment:   $activeDeployId"
    Write-Host "  Phase:               " -NoNewline

    switch ($phase) {
        "ACTIVE"     { Write-Host $phase -ForegroundColor Green }
        "DEPLOYING"  { Write-Host $phase -ForegroundColor Yellow }
        "ERROR"      { Write-Host $phase -ForegroundColor Red }
        default      { Write-Host $phase }
    }

    Write-Host "  Last Updated:        $updatedAt"
    Write-Host ""
}

function Get-Deployments {
    param([int]$Count = 10)

    Write-Host ""
    Write-Host "Recent Deployments ($APP_NAME)" -ForegroundColor Cyan
    Write-Host "─────────────────────────────────────────────────────────────────" -ForegroundColor DarkGray

    $deployments = doctl apps list-deployments $APP_ID --output json 2>&1 | ConvertFrom-Json

    $deployments | Select-Object -First $Count | ForEach-Object {
        $phase = $_.phase
        $color = switch ($phase) {
            "ACTIVE"     { "Green" }
            "DEPLOYING"  { "Yellow" }
            "PENDING"    { "Yellow" }
            "ERROR"      { "Red" }
            "CANCELED"   { "DarkGray" }
            default      { "White" }
        }

        $phaseStr = $phase.PadRight(12)
        $created = [datetime]::Parse($_.created_at).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
        Write-Host "  [$($_.id)]  " -NoNewline
        Write-Host $phaseStr -ForegroundColor $color -NoNewline
        Write-Host "  $created  " -NoNewline

        if ($_.cause) {
            Write-Host "  $($_.cause)" -ForegroundColor DarkGray
        } else {
            Write-Host ""
        }
    }

    Write-Host ""
}

function Get-DeploymentDetails {
    param([string]$DeploymentId)

    if (-not $DeploymentId) {
        # Get the latest deployment
        $deployments = doctl apps list-deployments $APP_ID --output json 2>&1 | ConvertFrom-Json
        $DeploymentId = $deployments[0].id
    }

    Write-Host ""
    Write-Host "Deployment Details" -ForegroundColor Cyan
    Write-Host "─────────────────────────────────────" -ForegroundColor DarkGray

    $d = doctl apps get-deployment $APP_ID $DeploymentId --output json 2>&1 | ConvertFrom-Json

    $phase = $d.phase
    $color = switch ($phase) {
        "ACTIVE"     { "Green" }
        "DEPLOYING"  { "Yellow" }
        "PENDING"    { "Yellow" }
        "ERROR"      { "Red" }
        "CANCELED"   { "DarkGray" }
        default      { "White" }
    }

    Write-Host "  ID:          $($d.id)"
    Write-Host "  Phase:       " -NoNewline; Write-Host $phase -ForegroundColor $color
    Write-Host "  Cause:       $($d.cause)"
    Write-Host "  Created:     $([datetime]::Parse($d.created_at).ToLocalTime().ToString('yyyy-MM-dd HH:mm:ss'))"

    if ($d.updated_at) {
        Write-Host "  Updated:     $([datetime]::Parse($d.updated_at).ToLocalTime().ToString('yyyy-MM-dd HH:mm:ss'))"
    }

    Write-Host ""

    # Show progress steps if deploying
    if ($d.progress) {
        Write-Host "  Progress:" -ForegroundColor Yellow
        $d.progress.steps | ForEach-Object {
            $stepPhase = $_.phase
            $stepColor = switch ($stepPhase) {
                "SUCCESS"    { "Green" }
                "RUNNING"    { "Yellow" }
                "ERROR"      { "Red" }
                "PENDING"    { "DarkGray" }
                default      { "White" }
            }
            Write-Host "    [$stepPhase] " -ForegroundColor $stepColor -NoNewline
            Write-Host $_.name
            if ($_.steps) {
                $_.steps | ForEach-Object {
                    $subPhase = $_.phase
                    $subColor = switch ($subPhase) {
                        "SUCCESS" { "Green" }
                        "RUNNING" { "Yellow" }
                        "ERROR"   { "Red" }
                        "PENDING" { "DarkGray" }
                        default   { "White" }
                    }
                    Write-Host "      [$subPhase] " -ForegroundColor $subColor -NoNewline
                    Write-Host $_.name
                }
            }
        }
        Write-Host ""
    }

    # Show services state
    if ($d.services) {
        Write-Host "  Services:" -ForegroundColor Yellow
        $d.services | ForEach-Object {
            Write-Host "    $($_.name): " -NoNewline
            Write-Host $_.phase -ForegroundColor $color
        }
        Write-Host ""
    }
}

function Get-Logs {
    param(
        [string]$Type = "run",
        [int]$Lines = 100,
        [switch]$Follow
    )

    $typeLabel = switch ($Type) {
        "run"    { "Runtime" }
        "build"  { "Build" }
        "deploy" { "Deploy" }
        default  { $Type }
    }

    if ($Follow) {
        Write-Host ""
        Write-Host "Following $typeLabel logs for $APP_NAME (Ctrl+C to stop)..." -ForegroundColor Cyan
        Write-Host "─────────────────────────────────────" -ForegroundColor DarkGray
        doctl apps logs $APP_ID $COMPONENT --type $Type --follow
    } else {
        Write-Host ""
        Write-Host "$typeLabel Logs for $APP_NAME (last $Lines lines)" -ForegroundColor Cyan
        Write-Host "─────────────────────────────────────" -ForegroundColor DarkGray
        doctl apps logs $APP_ID $COMPONENT --type $Type --tail $Lines
    }
}

function Watch-Build {
    $deployments = doctl apps list-deployments $APP_ID --output json 2>&1 | ConvertFrom-Json
    $latest = $deployments[0]
    $phase = $latest.phase

    if ($phase -notin @("BUILDING", "DEPLOYING", "PENDING")) {
        Write-Host ""
        Write-Host "No build in progress." -ForegroundColor DarkGray
        Write-Host "  Latest deployment [$($latest.id)] is in state: " -NoNewline
        $c = if ($phase -eq "ACTIVE") { "Green" } elseif ($phase -eq "ERROR") { "Red" } else { "White" }
        Write-Host $phase -ForegroundColor $c
        Write-Host ""
        return
    }

    Write-Host ""
    Write-Host "Build in progress for deployment [$($latest.id)] (phase: $phase)" -ForegroundColor Yellow
    Write-Host "Tailing build logs (Ctrl+C to stop)..." -ForegroundColor Cyan
    Write-Host "─────────────────────────────────────" -ForegroundColor DarkGray
    doctl apps logs $APP_ID $COMPONENT --type build --follow
}

function Watch-Deployment {
    Write-Host ""
    Write-Host "Watching latest deployment (refreshes every 10s, Ctrl+C to stop)..." -ForegroundColor Cyan

    while ($true) {
        Clear-Host
        Get-AppStatus
        Get-DeploymentDetails

        $deployments = doctl apps list-deployments $APP_ID --output json 2>&1 | ConvertFrom-Json
        $latestPhase = $deployments[0].phase

        if ($latestPhase -notin @("DEPLOYING", "PENDING", "BUILDING")) {
            Write-Host "Deployment has settled in state: " -NoNewline
            $c = if ($latestPhase -eq "ACTIVE") { "Green" } elseif ($latestPhase -eq "ERROR") { "Red" } else { "White" }
            Write-Host $latestPhase -ForegroundColor $c
            break
        }

        Write-Host "Still deploying... next refresh in 10s (Ctrl+C to stop)" -ForegroundColor Yellow
        Start-Sleep -Seconds 10
    }
}

function Show-Menu {
    while ($true) {
        Write-Host ""
        Write-Host "Nightingale Trading Production Monitor" -ForegroundColor Cyan
        Write-Host "══════════════════════════════════════" -ForegroundColor Cyan
        Write-Host "  1. App status"
        Write-Host "  2. Recent deployments"
        Write-Host "  3. Latest deployment details"
        Write-Host "  4. Runtime logs"
        Write-Host "  5. Build logs (latest deployment)"
        Write-Host "  6. Deploy logs (latest deployment)"
        Write-Host "  7. Follow runtime logs (live)"
        Write-Host "  8. Watch deployment (auto-refresh)"
        Write-Host "  9. Monitor build (tail if in progress)"
        Write-Host "  Q. Quit"
        Write-Host ""
        $choice = Read-Host "Choose"

        switch ($choice.ToUpper()) {
            "1" { Get-AppStatus }
            "2" { Get-Deployments }
            "3" { Get-DeploymentDetails }
            "4" { Get-Logs -Type "run" }
            "5" { Get-Logs -Type "build" }
            "6" { Get-Logs -Type "deploy" }
            "7" { Get-Logs -Type "run" -Follow }
            "8" { Watch-Deployment }
            "9" { Watch-Build }
            "Q" { Write-Host "Bye!" -ForegroundColor Green; return }
            default { Write-Host "Unknown option." -ForegroundColor Red }
        }
    }
}

# ── Entry point ──────────────────────────────────────────────────────────────

switch ($Command.ToLower()) {
    "logs"         { Get-Logs -Type "run"    -Lines $Lines }
    "logs-build"   { Get-Logs -Type "build"  -Lines $Lines }
    "logs-deploy"  { Get-Logs -Type "deploy" -Lines $Lines }
    "logs-follow"  { Get-Logs -Type "run"    -Follow }
    "build"        { Watch-Build }
    "deployments"  { Get-Deployments }
    "deployment"   { Get-DeploymentDetails -DeploymentId $DeploymentId }
    "status"       { Get-AppStatus }
    "watch"        { Watch-Deployment }
    "menu"         { Show-Menu }
    "help"         { Show-Help }
    default        { Write-Host "Unknown command: $Command" -ForegroundColor Red; Show-Help }
}
