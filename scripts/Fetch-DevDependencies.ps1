#!/usr/bin/env pwsh

[CmdletBinding()]
param(
    [string]$DestinationRoot = ""
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path $PSScriptRoot -Parent
if ([string]::IsNullOrWhiteSpace($DestinationRoot)) {
    $DestinationRoot = Join-Path $repoRoot '.local\BepInEx'
}

$zipUrl = 'https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.2/BepInEx_win_x64_5.4.23.2.zip'
$zipPath = Join-Path $DestinationRoot 'BepInEx_win_x64_5.4.23.2.zip'
$extractPath = Join-Path $DestinationRoot 'extract'
$corePath = Join-Path $extractPath 'BepInEx\core'

New-Item -ItemType Directory -Path $DestinationRoot -Force | Out-Null

if (-not (Test-Path $zipPath)) {
    Invoke-WebRequest -Uri $zipUrl -OutFile $zipPath
}

if (-not (Test-Path $corePath)) {
    if (Test-Path $extractPath) {
        Remove-Item -Path $extractPath -Recurse -Force
    }

    Expand-Archive -LiteralPath $zipPath -DestinationPath $extractPath -Force
}

Write-Host "BepInEx core ready at: $corePath"
