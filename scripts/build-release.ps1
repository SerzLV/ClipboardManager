param(
    [string]$Version = "0.0.0-local",
    [string]$Runtime = "win-x64",
    [switch]$SkipInstaller
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$projectPath = Join-Path $repoRoot "ClipboardManager\ClipboardManager.csproj"
$artifactsDir = Join-Path $repoRoot "artifacts"
$publishDir = Join-Path $artifactsDir "publish\$Runtime"
$installerDir = Join-Path $artifactsDir "installer"
$portableZip = Join-Path $artifactsDir "ClipboardManager-$Version-$Runtime-portable.zip"
$innoScript = Join-Path $repoRoot "installer\ClipboardManager.iss"

function Resolve-InnoSetupCompiler {
    $candidates = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
    )

    foreach ($candidate in $candidates) {
        if ($candidate -and (Test-Path -LiteralPath $candidate)) {
            return $candidate
        }
    }

    return $null
}

New-Item -ItemType Directory -Force -Path $artifactsDir | Out-Null
if (Test-Path -LiteralPath $publishDir) {
    Remove-Item -LiteralPath $publishDir -Recurse -Force
}
if (Test-Path -LiteralPath $portableZip) {
    Remove-Item -LiteralPath $portableZip -Force
}
New-Item -ItemType Directory -Force -Path $publishDir | Out-Null

dotnet publish $projectPath `
    --configuration Release `
    --runtime $Runtime `
    --self-contained true `
    --output $publishDir `
    -p:Version=$Version `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=None `
    -p:DebugSymbols=false

Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $portableZip -Force
Write-Host "Portable package: $portableZip"

if ($SkipInstaller) {
    return
}

$iscc = Resolve-InnoSetupCompiler
if (-not $iscc) {
    Write-Warning "Inno Setup 6 was not found. Install it to build the setup EXE, or use the portable ZIP."
    return
}

New-Item -ItemType Directory -Force -Path $installerDir | Out-Null
& $iscc `
    "/DAppVersion=$Version" `
    "/DSourceDir=$publishDir" `
    "/DOutputDir=$installerDir" `
    $innoScript

Write-Host "Installer output: $installerDir"
