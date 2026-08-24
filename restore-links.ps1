# Restores the directory symbolic links the repository is built on (every project's Data/Shared and
# Main's Addons/*). A Windows clone or worktree made without Developer Mode writes them out as plain
# text files instead, and the projects then load nothing. Run once after cloning, after adding a
# worktree, or whenever a shared folder looks empty. -Check only reports and exits non-zero.
#
# The list of links is read from git (index entries with mode 120000), so a link added to the
# repository is picked up here without editing this script.
[CmdletBinding()]
param(
    [switch]$Check
)

$ErrorActionPreference = 'Stop'

$SymlinkMode = '120000'

function Invoke-Git {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$Arguments)

    $output = & git @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw ('"git {0}" failed with exit code {1}.' -f ($Arguments -join ' '), $LASTEXITCODE)
    }
    return $output
}

# Windows spells its paths with backslashes and git with forward ones; everything below compares
# Windows spelling, so both the index paths and the link targets are converted on the way in.
function ConvertTo-WindowsPath {
    param([string]$Path)

    $converted = $Path -replace '/', '\'
    if ($converted.StartsWith('.\')) { $converted = $converted.Substring(2) }
    return $converted.TrimEnd('\')
}

function Get-TrackedLink {
    param([string]$RepositoryRoot)

    $links = @()
    foreach ($entry in (Invoke-Git -Arguments @('-C', $RepositoryRoot, '-c', 'core.quotePath=false', 'ls-files', '-s'))) {
        if (-not $entry.StartsWith($SymlinkMode + ' ')) { continue }

        # "120000 <sha> <stage>\t<path>": the tab is the only separator a path cannot contain.
        $split = $entry.Split([char]9, 2)
        $blob = $split[0].Split(' ')[1]
        $relativePath = $split[1]

        # The blob of a symbolic link is its target, relative to the folder that holds the link.
        $target = @(Invoke-Git -Arguments @('-C', $RepositoryRoot, 'cat-file', 'blob', $blob))[0]

        $links += [pscustomobject]@{
            RelativePath = $relativePath
            FullPath     = Join-Path $RepositoryRoot (ConvertTo-WindowsPath $relativePath)
            Target       = ConvertTo-WindowsPath $target
            GitTarget    = $target
        }
    }
    return $links
}

function Write-Result {
    param([string]$Status, [string]$Colour, [string]$Link, [string]$Detail)

    $line = '  {0,-9} {1}' -f $Status, $Link
    if ($Detail) { $line = '{0}  ({1})' -f $line, $Detail }
    Write-Host $line -ForegroundColor $Colour
}

function New-DirectoryLink {
    param([string]$FullPath, [string]$Target)

    $holder = Split-Path -Parent $FullPath
    New-Item -ItemType Directory -Force -Path $holder | Out-Null

    # Windows PowerShell writes a symbolic link for an administrator only, Developer Mode or not;
    # mklink is what asks for the unprivileged link Developer Mode allows. A junction would record an
    # absolute path and break on the next machine.
    & cmd.exe /c mklink /D "$FullPath" "$Target" | Out-Null
    if ($LASTEXITCODE -ne 0) {
        $failure = 'Cannot link "{0}" -> "{1}" (mklink exit code {2}). A symbolic link needs Developer Mode ' +
                   '(Settings > System > For developers) or an elevated shell.'
        throw ($failure -f $FullPath, $Target, $LASTEXITCODE)
    }
}

$repositoryRoot = (Invoke-Git -Arguments @('-C', $PSScriptRoot, 'rev-parse', '--show-toplevel')) -replace '/', '\'
$links = Get-TrackedLink -RepositoryRoot $repositoryRoot
if ($links.Count -eq 0) { throw ('No symbolic links are tracked in "{0}".' -f $repositoryRoot) }

if ($Check) { Write-Host ('checking {0} links in {1}' -f $links.Count, $repositoryRoot) }
else { Write-Host ('restoring {0} links in {1}' -f $links.Count, $repositoryRoot) }

$ok = 0
$changed = 0
$blocked = @()

foreach ($link in $links) {
    $holder = Split-Path -Parent $link.FullPath
    $targetFullPath = [System.IO.Path]::GetFullPath((Join-Path $holder $link.Target))
    $existing = Get-Item -LiteralPath $link.FullPath -Force -ErrorAction SilentlyContinue

    $isLink = $existing -and ($existing.Attributes -band [System.IO.FileAttributes]::ReparsePoint)

    # A real folder here is somebody's data, not a leftover link, and is never removed: telling the
    # difference wrong once would take the shared content with it.
    $isRealFolder = $existing -and ($existing -is [System.IO.DirectoryInfo]) -and (-not $isLink)
    if ($isRealFolder) {
        Write-Result -Status 'BLOCKED' -Colour 'Red' -Link $link.RelativePath -Detail 'a real folder is in the way, move it aside by hand'
        $blocked += $link.RelativePath
        continue
    }

    # Checked before the link itself, so a link that spells the right target but resolves to nothing
    # is reported instead of passing as healthy: re-creating it would not help either way.
    if (-not (Test-Path -LiteralPath $targetFullPath)) {
        Write-Result -Status 'BLOCKED' -Colour 'Red' -Link $link.RelativePath -Detail ('target "{0}" does not exist' -f $link.GitTarget)
        $blocked += $link.RelativePath
        continue
    }

    if ($isLink -and ((ConvertTo-WindowsPath (@($existing.Target)[0])) -eq $link.Target)) {
        Write-Result -Status 'ok' -Colour 'DarkGray' -Link $link.RelativePath -Detail $link.GitTarget
        $ok++
        continue
    }

    $reason = 'missing'
    if ($isLink) { $reason = 'points at "{0}" instead of "{1}"' -f (@($existing.Target)[0]), $link.GitTarget }
    elseif ($existing) { $reason = 'a plain file holds the link target as text' }

    if ($Check) {
        Write-Result -Status 'WRONG' -Colour 'Yellow' -Link $link.RelativePath -Detail $reason
        $blocked += $link.RelativePath
        continue
    }

    if ($isLink) {
        # Only the link itself goes. A recursive delete would walk into the shared folder and take
        # the repository content with it.
        [System.IO.Directory]::Delete($existing.FullName)
    }
    elseif ($existing) {
        # Git without symbolic link rights writes the target as the file's text: no data of its own.
        Remove-Item -LiteralPath $existing.FullName -Force
    }

    New-DirectoryLink -FullPath $link.FullPath -Target $link.Target
    Write-Result -Status 'restored' -Colour 'Cyan' -Link $link.RelativePath -Detail $reason
    $changed++
}

if ($Check) {
    Write-Host ('{0} links: {1} ok, {2} to restore' -f $links.Count, $ok, $blocked.Count)
    if ($blocked.Count -gt 0) {
        Write-Host 'run restore-links.ps1 without -Check to repair.' -ForegroundColor Yellow
        exit 1
    }
    exit 0
}

Write-Host ('{0} links: {1} already in place, {2} restored, {3} blocked' -f $links.Count, $ok, $changed, $blocked.Count)
if ($blocked.Count -gt 0) {
    Write-Host ('Blocked: {0}' -f ($blocked -join ', ')) -ForegroundColor Red
    exit 1
}
exit 0
