# Builds the Ready Typing trainer, its installer, and the dist folder.
#
# Usage:
#   .\build-setup.ps1
#   .\build-setup.ps1 -Dotnet 'C:\path\to\dotnet.exe'
#   .\build-setup.ps1 -SkipPublish   # reuse artifacts\publish

[CmdletBinding()]
param(
    [string]$Dotnet = '',
    [string]$Runtime = 'win-x64',
    [switch]$SkipPublish
)

$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot
$appProject = Join-Path $root 'BijoyTrainer\BijoyTrainer.csproj'
$setupProject = Join-Path $root 'ReadyTypingSetup\ReadyTypingSetup.csproj'
$setupDirectory = Join-Path $root 'ReadyTypingSetup'
$publishDirectory = Join-Path $root 'artifacts\publish'
$payloadDirectory = Join-Path $setupDirectory 'Payload'
$payloadPath = Join-Path $payloadDirectory 'ReadyTyping.exe.gz'
$distDirectory = Join-Path $root 'dist'

if (-not $Dotnet) {
    $command = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($command) {
        $Dotnet = $command.Source
    }
    else {
        $fallback = Join-Path $env:LOCALAPPDATA 'Temp\opencode\dotnet8\dotnet.exe'
        if (Test-Path $fallback) {
            $Dotnet = $fallback
        }
        else {
            throw 'dotnet was not found. Pass -Dotnet with the full path.'
        }
    }
}

Write-Host "Using $Dotnet"

if (-not $SkipPublish) {
    Write-Host 'Publishing the self-contained trainer...'
    & $Dotnet publish $appProject `
        -c Release `
        -r $Runtime `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:EnableCompressionInSingleFile=true `
        -o $publishDirectory `
        -v minimal

    if ($LASTEXITCODE -ne 0) {
        throw 'The trainer publish failed.'
    }
}

$appExe = Join-Path $publishDirectory 'BijoyTrainer.exe'
if (-not (Test-Path $appExe)) {
    throw "Published executable not found: $appExe"
}

New-Item -ItemType Directory -Force -Path $payloadDirectory | Out-Null
Write-Host 'Compressing the installer payload...'
$source = [IO.File]::OpenRead($appExe)
$target = [IO.File]::Create($payloadPath)
$gzip = New-Object IO.Compression.GZipStream(
    $target,
    [IO.Compression.CompressionLevel]::Optimal)
try {
    $source.CopyTo($gzip)
}
finally {
    $gzip.Dispose()
    $target.Dispose()
    $source.Dispose()
}

Write-Host 'Building the setup window...'
& $Dotnet build $setupProject -c Release -v minimal
if ($LASTEXITCODE -ne 0) {
    throw 'The installer build failed.'
}

$setupExe = Join-Path $setupDirectory 'bin\Release\net48\ReadyTypingSetup.exe'
if (-not (Test-Path $setupExe)) {
    throw "Setup executable not found: $setupExe"
}

New-Item -ItemType Directory -Force -Path $distDirectory | Out-Null
Copy-Item $appExe (Join-Path $distDirectory 'ReadyTyping.exe') -Force
Copy-Item $setupExe (Join-Path $distDirectory 'ReadyTypingSetup.exe') -Force

$appSize = [math]::Round((Get-Item (Join-Path $distDirectory 'ReadyTyping.exe')).Length / 1MB, 2)
$setupSize = [math]::Round((Get-Item (Join-Path $distDirectory 'ReadyTypingSetup.exe')).Length / 1MB, 2)
Write-Host ''
Write-Host "Done. dist\ReadyTyping.exe = $appSize MB, dist\ReadyTypingSetup.exe = $setupSize MB"
