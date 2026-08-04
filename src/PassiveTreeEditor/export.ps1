<#
.SYNOPSIS
    Builds the passive tree editor into Export/ and points it at the repository's SharedData.

.DESCRIPTION
    Two steps and a link. The C# solution is built first so a compile error is read as a compile error
    rather than as a failed export; then Godot writes the binary headlessly from the "Windows Desktop"
    preset. The exported tool reads its catalogs from a folder beside the binary, so the last step
    junctions Export/Data/Shared onto src/SharedData: edits made in the exported tool land in the
    repository, exactly as they do when the tool runs from the editor.

    Only the release template is what this exports with — a debug export needs a debug template
    installed next to it.

.PARAMETER Godot
    The Godot executable to export with. Defaults to $env:GODOT, then to the console build of the
    engine this project lives under.

.PARAMETER Preset
    Name of the preset in export_presets.cfg.
#>
[CmdletBinding()]
param(
    [string]$Godot = $env:GODOT,
    [string]$Preset = 'Windows Desktop'
)

$ErrorActionPreference = 'Stop'

$project = $PSScriptRoot
$solution = Join-Path $project 'PassiveTreeEditor.sln'
$exportDir = Join-Path $project 'Export'
$binary = Join-Path $exportDir 'PassiveTreeEditor.exe'
$package = Join-Path $exportDir 'PassiveTreeEditor.pck'
$assemblies = Join-Path $exportDir 'data_PassiveTreeEditor_windows_x86_64'
$dataLink = Join-Path $exportDir 'Data\Shared'
$sharedData = Join-Path $project '..\SharedData'

if (-not $Godot) {
    # The tool sits four levels below the engine install it is edited with. The console build is the
    # one worth calling: the windowed build writes its errors nowhere this script can read them.
    $Godot = Join-Path $project '..\..\..\..\Godot_v4.7-stable_mono_win64_console.exe'
}

if (-not (Test-Path $Godot)) {
    throw ('Godot not found at "{0}". Set the GODOT environment variable to the console executable.' -f $Godot)
}

if (-not (Test-Path $sharedData)) {
    throw ('SharedData not found at "{0}".' -f $sharedData)
}

$Godot = (Resolve-Path $Godot).Path
$sharedData = (Resolve-Path $sharedData).Path

Write-Host "building $solution (ExportRelease)" -ForegroundColor Cyan
& dotnet build $solution -c ExportRelease --nologo
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed with exit code $LASTEXITCODE." }

New-Item -ItemType Directory -Force -Path $exportDir | Out-Null

# The output folder sits inside the Godot project and holds a link to SharedData, so without this the
# engine scans the whole shared library a second time through the link and reports every asset as a
# duplicate UID. A .gdignore keeps the scanner out of the folder the build writes into.
$ignore = Join-Path $exportDir '.gdignore'
if (-not (Test-Path $ignore)) { New-Item -ItemType File -Path $ignore | Out-Null }

$embedded = Select-String -Path (Join-Path $project 'export_presets.cfg') -Pattern 'dotnet/embed_build_outputs\s*=\s*true' -Quiet

# A folder left over from an export that wrote its assemblies loose would sit next to a binary that
# now carries them inside the pack — two copies of the runtime, and no telling which one is read.
# Worth stopping over: the export that follows would look perfectly successful either way.
if ($embedded -and (Test-Path $assemblies)) {
    try { Remove-Item $assemblies -Recurse -Force -ErrorAction Stop }
    catch {
        throw ('Cannot delete the stale assembly folder "{0}": {1}' -f $assemblies, $_.Exception.Message +
               ' Close anything holding it — an Explorer window showing the folder is enough — and run again.')
    }
}

Write-Host "exporting `"$Preset`" with $Godot" -ForegroundColor Cyan
& $Godot --headless --path $project --export-release $Preset $binary
if ($LASTEXITCODE -ne 0) { throw "Godot export failed with exit code $LASTEXITCODE." }

# Godot reports a failed export in its log and still leaves whatever it managed to write behind, so
# the artefacts are checked rather than assumed. The managed assemblies are what goes missing quietly:
# without them the binary starts and dies before the first frame.
foreach ($artefact in @($binary, $package)) {
    if (-not (Test-Path $artefact)) { throw ('Export produced no "{0}".' -f $artefact) }
}

if ($embedded) {
    # Embedded, there is no folder to look at, so the pack is asked whether it carries them: an embed
    # that did not happen leaves an export that looks successful in every other way.
    $pack = [System.IO.File]::ReadAllText($package, [System.Text.Encoding]::GetEncoding(28591))
    if ($pack.IndexOf('PassiveTreeEditor.dll') -lt 0) {
        throw ('"{0}" carries no managed assemblies.' -f $package)
    }
}
elseif (-not (Test-Path $assemblies)) {
    throw ('Export produced no "{0}".' -f $assemblies)
}

if (-not (Test-Path $dataLink)) {
    New-Item -ItemType Directory -Force -Path (Split-Path $dataLink) | Out-Null
    New-Item -ItemType Junction -Path $dataLink -Target $sharedData | Out-Null
    Write-Host "linked $dataLink -> $sharedData" -ForegroundColor Cyan
}

# The link is stepped around on purpose: recursing through it would measure the whole repository.
$written = @((Get-Item $binary), (Get-Item $package))
if (Test-Path $assemblies) { $written += Get-ChildItem $assemblies -Recurse -File }
$size = [math]::Round((($written | Measure-Object Length -Sum).Sum / 1MB), 1)
Write-Host "done: $binary ($size MB written)" -ForegroundColor Green
