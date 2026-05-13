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

$bepInExVersion = '5.4.23.5'
$zipName = "BepInEx_win_x64_$bepInExVersion.zip"
$zipUrl = "https://github.com/BepInEx/BepInEx/releases/download/v$bepInExVersion/$zipName"
$zipPath = Join-Path $DestinationRoot $zipName
$extractPath = Join-Path $DestinationRoot 'extract'
$corePath = Join-Path $extractPath 'BepInEx\core'
$versionStampPath = Join-Path $extractPath '.bepinex-version'

New-Item -ItemType Directory -Path $DestinationRoot -Force | Out-Null

if (-not (Test-Path $zipPath)) {
    Invoke-WebRequest -Uri $zipUrl -OutFile $zipPath
}

$needsExtract = -not (Test-Path $corePath)

if (-not $needsExtract) {
    $cachedVersion = ''
    if (Test-Path $versionStampPath) {
        $cachedVersion = (Get-Content -LiteralPath $versionStampPath -Raw).Trim()
    }

    $needsExtract = $cachedVersion -ne $bepInExVersion
}

if ($needsExtract) {
    if (Test-Path $extractPath) {
        Remove-Item -Path $extractPath -Recurse -Force
    }

    Expand-Archive -LiteralPath $zipPath -DestinationPath $extractPath -Force
    Set-Content -LiteralPath $versionStampPath -Value $bepInExVersion -NoNewline
}

Write-Host "BepInEx core ready at: $corePath"
