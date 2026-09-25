# Tool exports

DataEditor and NarrativeEditor use the PassiveTreeEditor export layout: each project's `Export/`
contains its EXE and a separate PCK with embedded managed assemblies and the .NET runtime.
The Windows Desktop presets target `Export/<Project>.exe` and use installed Godot templates.

From the repository root:

```powershell
./src/Tooling/export.ps1
./src/Tooling/export.ps1 -Project DataEditor
./src/Tooling/export.ps1 -Project NarrativeEditor
```

The shared script builds each solution in ExportRelease and exports the tools sequentially.
It creates `.gdignore` before exporting so Godot does not scan generated output or duplicate assets.
It checks the process exit code, both output files and the embedded managed assembly names.
Export logs remain in the corresponding Export directory. Generated exports are excluded from Git.

`Export/Data/Shared` is a relative symbolic link to the repository's `src/SharedData`. Creating it
requires Windows Developer Mode or an elevated shell. Editing through the exported tools writes
the same catalogs as editing through Godot. Outside this checkout, supply an actual Data/Shared
folder or pass `--data <path>`; keep the EXE and PCK together.

Only the Godot 4.7 mono Windows release template is installed on the current workstation.
The script explicitly uses `--export-release`; when exporting from Godot's dialog, clear
**Export With Debug**. A missing destination directory is created by the script before export.
Use `-Godot <path>` or `GODOT` for a different engine installation.

PassiveTreeEditor keeps its existing script and preset. No game data or runtime game behavior
changes are part of this tooling export configuration.

## Validation (2026-09-22)

Both configured Release exports completed successfully. Their EXE/PCK pairs contain the expected
managed assemblies and start from an unrelated working directory without `--data` (exit code 0).
Both SharedData links resolve to the repository catalogs. NarrativeEditor reported no startup
errors. DataEditor's headless check logged a popup-position error while displaying catalog
validation notes; this is separate from export and managed runtime initialization.
