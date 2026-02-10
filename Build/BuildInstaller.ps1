param(
    [string]$SourceDir
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$isccExe = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
$versionFile = Join-Path $scriptDir "..\EddyLib\GlobalSettings\EddyVersion.cs"

if ($SourceDir) {
    $SourceDir = $SourceDir.Trim().Trim('"')
}

if (-not (Test-Path $isccExe)) {
    Write-Error "Inno Setup compiler not found at '$isccExe'."
}

if (-not $SourceDir) {
    $candidateRelativePaths = @(
        "..\Eddy\bin\Release\net8.0-windows\Eddy.gha",
        "..\Eddy\bin\Debug\net8.0-windows\Eddy.gha",
        "..\Eddy\bin\TestBuild\Eddy.gha",
        "..\Eddy\bin\Eddy.gha"
    )

    $existingCandidates = foreach ($relativePath in $candidateRelativePaths) {
        $fullPath = [System.IO.Path]::GetFullPath((Join-Path $scriptDir $relativePath))
        if (Test-Path $fullPath) {
            Get-Item $fullPath
        }
    }

    $latestCandidate = $existingCandidates | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $latestCandidate) {
        Write-Error "Could not locate Eddy.gha build output."
    }

    $SourceDir = $latestCandidate.DirectoryName
}

$sourceDirFull = [System.IO.Path]::GetFullPath($SourceDir)
$srcApp = Join-Path $sourceDirFull "Eddy.gha"

if (-not (Test-Path $srcApp)) {
    Write-Error "Build artifact not found: '$srcApp'."
}

$appVer = $null
if (Test-Path $versionFile) {
    $versionText = Get-Content $versionFile -Raw
    $match = [regex]::Match($versionText, 'ProductVersion\s*=\s*"([^"]+)"')
    if ($match.Success) {
        $appVer = $match.Groups[1].Value
    }
}

if (-not $appVer) {
    $appVer = (Get-Item $srcApp).VersionInfo.FileVersion
}

Write-Host ""
Write-Host "Source build folder: '$sourceDirFull'"
Write-Host "Installer version:   '$appVer'"
Write-Host ""

$issFile = Join-Path $scriptDir "BuildInstaller.iss"
& $isccExe "/Qp" "/DSrcDir=$sourceDirFull" "/DSrcApp=$srcApp" "/DAppVerStr=$appVer" $issFile
exit $LASTEXITCODE
