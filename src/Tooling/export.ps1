<#
.SYNOPSIS
    Exports the data and narrative editors using the PassiveTreeEditor layout.
.DESCRIPTION
    Builds ExportRelease, writes Export/<Project>.exe and .pck with embedded .NET assemblies,
    and links Export/Data/Shared to the repository catalogs. Requires PowerShell 7 and Windows
    Developer Mode or an elevated shell for symbolic links. Uses the installed release template.
#>
[CmdletBinding()]
param(
    [ValidateSet('DataEditor', 'NarrativeEditor')]
    [string[]]$Project = @('DataEditor', 'NarrativeEditor'),
    [string]$Godot = $env:GODOT
)

$ErrorActionPreference = 'Stop'
if (-not $Godot) {
    $Godot = Join-Path $PSScriptRoot '..\..\..\..\Godot_v4.7-stable_mono_win64.exe'
}
$Godot = (Resolve-Path -LiteralPath $Godot).Path
$sharedData = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\SharedData')).Path

foreach ($name in $Project) {
    $projectDir = Join-Path $PSScriptRoot $name
    $exportDir = Join-Path $projectDir 'Export'
    $binary = Join-Path $exportDir "$name.exe"
    $package = Join-Path $exportDir "$name.pck"
    $looseAssemblies = Join-Path $exportDir "data_${name}_windows_x86_64"
    if (Test-Path -LiteralPath $looseAssemblies) {
        throw "Move the old loose assembly folder aside before exporting: $looseAssemblies"
    }

    $null = New-Item -ItemType Directory -Path $exportDir -Force
    [IO.File]::WriteAllText((Join-Path $exportDir '.gdignore'), '')
    $dataDir = Join-Path $exportDir 'Data'
    $null = New-Item -ItemType Directory -Path $dataDir -Force
    $linkPath = Join-Path $dataDir 'Shared'
    $relativeTarget = [IO.Path]::GetRelativePath($dataDir, $sharedData)
    $existing = Get-Item -LiteralPath $linkPath -Force -ErrorAction SilentlyContinue
    if ($existing) {
        if ($existing.LinkType -ne 'SymbolicLink' -or @($existing.Target)[0] -ne $relativeTarget) {
            throw "Expected a relative SharedData symbolic link at $linkPath; move the existing entry aside."
        }
    }
    else {
        Push-Location $dataDir
        try { $null = New-Item -ItemType SymbolicLink -Path $linkPath -Target $relativeTarget }
        finally { Pop-Location }
    }

    Write-Host "Building $name (ExportRelease)"
    & dotnet build (Join-Path $projectDir "$name.sln") -c ExportRelease --nologo
    if ($LASTEXITCODE -ne 0) { throw "Build failed for $name (exit $LASTEXITCODE)." }

    # Wait for the windowed executable explicitly; PowerShell does not await GUI applications by default.
    $arguments = @('--headless', '--path', ('"{0}"' -f $projectDir), '--export-release', '"Windows Desktop"')
    $process = Start-Process -FilePath $Godot -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru `
        -RedirectStandardOutput (Join-Path $exportDir 'export.stdout.log') `
        -RedirectStandardError (Join-Path $exportDir 'export.stderr.log')
    if ($process.ExitCode -ne 0) {
        Get-Content -LiteralPath (Join-Path $exportDir 'export.stderr.log')
        throw "Export failed for $name (exit $($process.ExitCode))."
    }

    foreach ($artifact in @($binary, $package)) {
        if (-not (Test-Path -LiteralPath $artifact)) { throw "Export produced no $artifact" }
    }
    $pack = [IO.File]::ReadAllText($package, [Text.Encoding]::GetEncoding(28591))
    foreach ($assembly in @("$name.dll", 'GodotSharp.dll', 'Core.dll', 'LastBreath.Descriptors.dll', 'Tooling.Core.dll')) {
        if (-not $pack.Contains($assembly)) { throw "Missing embedded assembly $assembly in $package" }
    }
    $sizeMb = [math]::Round(((Get-Item -LiteralPath $binary).Length + (Get-Item -LiteralPath $package).Length) / 1MB, 1)
    Write-Host "Exported $binary ($sizeMb MB); SharedData -> $relativeTarget"
}
