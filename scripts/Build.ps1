[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [ValidateSet('win-x64', 'linux-arm64', 'linux-arm')]
    [string]$Runtime = 'win-x64',

    [switch]$NoStart
)

$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $PSScriptRoot
$outputRoot = Join-Path $projectRoot 'output'
$currentDirectory = Join-Path $outputRoot "current\$Runtime"
$archiveRoot = Join-Path $outputRoot 'archive'
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$buildId = "1.0.0+$timestamp.$Runtime"
$archiveDirectory = Join-Path $archiveRoot "$timestamp-$Runtime"
$stagingDirectory = Join-Path $outputRoot ".staging\$timestamp-$Runtime"
$projectFile = Join-Path $projectRoot 'src\DmdClock.App\DmdClock.App.csproj'

$localDotnet = Join-Path (Split-Path -Parent $projectRoot) '.tools\dotnet10\dotnet.exe'
$dotnet = if ($env:DMD_DOTNET) {
    $env:DMD_DOTNET
} elseif (Test-Path -LiteralPath $localDotnet) {
    $localDotnet
} else {
    'dotnet'
}

function Assert-WithinOutputRoot([string]$Path) {
    $resolvedOutput = [IO.Path]::GetFullPath($outputRoot).TrimEnd([IO.Path]::DirectorySeparatorChar)
    $resolvedPath = [IO.Path]::GetFullPath($Path)
    if (-not $resolvedPath.StartsWith("$resolvedOutput$([IO.Path]::DirectorySeparatorChar)", [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing operation outside output directory: $resolvedPath"
    }
}

function Stop-DmdClockProcesses {
    $targets = @(Get-CimInstance Win32_Process | Where-Object {
        $_.Name -in @('DmdClock.App.exe', 'DmdClock.Tools.exe') -or
        ($_.Name -eq 'dotnet.exe' -and
            ($_.CommandLine -like '*DmdClock.App*' -or $_.CommandLine -like '*DmdClock.Tools*'))
    })

    foreach ($target in $targets) {
        try {
            $process = Get-Process -Id $target.ProcessId -ErrorAction Stop
            $gracefulRequested = $process.MainWindowHandle -ne 0 -and $process.CloseMainWindow()
            if ($gracefulRequested) {
                $null = $process.WaitForExit(3000)
            }
            if (-not $process.HasExited) {
                Stop-Process -Id $process.Id -Force -ErrorAction Stop
                $process.WaitForExit()
                Write-Host "Stopped locked DMDClock process: $($target.Name) (PID $($target.ProcessId))"
            } else {
                Write-Host "Closed DMDClock process gracefully: $($target.Name) (PID $($target.ProcessId))"
            }
        }
        catch [Microsoft.PowerShell.Commands.ProcessCommandException] {
            # The process already exited between discovery and shutdown.
        }
    }
}

Assert-WithinOutputRoot $currentDirectory
Assert-WithinOutputRoot $archiveDirectory
Assert-WithinOutputRoot $stagingDirectory

Stop-DmdClockProcesses

New-Item -ItemType Directory -Force -Path (Split-Path -Parent $stagingDirectory) | Out-Null

try {
    & $dotnet publish $projectFile `
        --configuration $Configuration `
        --runtime $Runtime `
        --self-contained true `
        "-p:InformationalVersion=$buildId" `
        --output $stagingDirectory

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE."
    }

    $stagingScenes = Join-Path $stagingDirectory 'scenes'
    $currentScenes = Join-Path $currentDirectory 'scenes'
    $projectScenes = Join-Path $projectRoot 'scenes'
    $sceneSource = if ((Test-Path -LiteralPath $currentScenes) -and
        (Get-ChildItem -LiteralPath $currentScenes -File -Recurse -Filter *.scn | Select-Object -First 1)) {
        $currentScenes
    } else {
        $projectScenes
    }
    New-Item -ItemType Directory -Force -Path $stagingScenes | Out-Null
    if (Test-Path -LiteralPath $sceneSource) {
        Get-ChildItem -LiteralPath $sceneSource -Force |
            Copy-Item -Destination $stagingScenes -Recurse -Force
    }
    $currentMetadata = Join-Path $currentScenes 'scene-metadata.json'
    $projectMetadata = Join-Path $projectScenes 'scene-metadata.json'
    $metadataSource = if ((Test-Path -LiteralPath $currentMetadata) -and (Test-Path -LiteralPath $projectMetadata)) {
        $currentInfo = Get-Item -LiteralPath $currentMetadata
        $projectInfo = Get-Item -LiteralPath $projectMetadata
        if ($currentInfo.LastWriteTimeUtc -gt $projectInfo.LastWriteTimeUtc) { $currentMetadata } else { $projectMetadata }
    } elseif (Test-Path -LiteralPath $currentMetadata) {
        $currentMetadata
    } else {
        $projectMetadata
    }
    if (Test-Path -LiteralPath $metadataSource) {
        Copy-Item -LiteralPath $metadataSource -Destination (Join-Path $stagingScenes 'scene-metadata.json') -Force
    }

    $toolsProject = Join-Path $projectRoot 'tools\DmdClock.Tools\DmdClock.Tools.csproj'
    $compatibilityOutput = & $dotnet run --project $toolsProject `
        --configuration $Configuration -- scan $stagingScenes
    if ($LASTEXITCODE -ne 0) {
        throw "SCN compatibility scan failed with exit code $LASTEXITCODE."
    }
    $compatibilityReport = @(
        'DMDClock SCN compatibility report'
        "Generated: $((Get-Date).ToString('o'))"
        "Source: .\scenes"
        ''
        $compatibilityOutput
    )
    Set-Content -LiteralPath (Join-Path $stagingDirectory 'SCN-COMPATIBILITY.txt') `
        -Value $compatibilityReport -Encoding utf8

    $buildInfo = [ordered]@{
        buildId = $buildId
        builtAt = (Get-Date).ToString('o')
        runtime = $Runtime
        configuration = $Configuration
        selfContained = $true
    } | ConvertTo-Json
    Set-Content -LiteralPath (Join-Path $stagingDirectory 'build-info.json') `
        -Value $buildInfo -Encoding utf8

    if ((Test-Path -LiteralPath $currentDirectory) -and
        (Get-ChildItem -LiteralPath $currentDirectory -Force | Select-Object -First 1)) {
        New-Item -ItemType Directory -Force -Path $archiveDirectory | Out-Null
        Get-ChildItem -LiteralPath $currentDirectory -Force |
            Copy-Item -Destination $archiveDirectory -Recurse -Force

        $archiveManifest = [ordered]@{
            archivedAt = (Get-Date).ToString('o')
            runtime = $Runtime
            configuration = $Configuration
            source = $currentDirectory
        } | ConvertTo-Json
        Set-Content -LiteralPath (Join-Path $archiveDirectory 'archive-manifest.json') -Value $archiveManifest -Encoding utf8

        Write-Host "Archived previous build to: $archiveDirectory"
    }

    if (Test-Path -LiteralPath $currentDirectory) {
        Remove-Item -LiteralPath $currentDirectory -Recurse -Force
    }

    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $currentDirectory) | Out-Null
    Move-Item -LiteralPath $stagingDirectory -Destination $currentDirectory
    Write-Host "Published new build to: $currentDirectory"

    if ($Runtime -eq 'win-x64' -and -not $NoStart) {
        $publishedExe = Join-Path $currentDirectory 'DmdClock.App.exe'
        $startedProcess = Start-Process -FilePath $publishedExe -WorkingDirectory $currentDirectory -PassThru
        Write-Host "Started new build: $publishedExe (PID $($startedProcess.Id))"
    }
}
finally {
    if (Test-Path -LiteralPath $stagingDirectory) {
        Remove-Item -LiteralPath $stagingDirectory -Recurse -Force
    }
}
