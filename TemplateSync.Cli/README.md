# TemplateSync.Cli

Command-line utility to compare and optionally sync Eddy component input/output labels (`Name` + `NickName`) in `.ghx` template files.
It works against a local clone of the templates repository.

## Usage

```bash
dotnet run --project TemplateSync.Cli -- --repo-dir /path/to/Eddy3D-OutdoorIndoorTemplates
```

Sync/fix mode:

```bash
dotnet run --project TemplateSync.Cli -- --repo-dir /path/to/Eddy3D-OutdoorIndoorTemplates --fix
```

Count + label reconciliation mode:

```bash
dotnet run --project TemplateSync.Cli -- --repo-dir /path/to/Eddy3D-OutdoorIndoorTemplates --fix-counts --fix
```

If the repo directory does not exist, the tool clones it from GitHub.
If it exists, the tool fetches/pulls and checks out the branch matching `EddyVersion.ProductVersion` by default.

Default repo location is dynamic and cross-platform:
- macOS: `/Users/<user>/Documents/GitHub/Eddy3D-Dev/Eddy3D-OutdoorIndoorTemplates`
- Windows: `C:\Users\<user>\Documents\GitHub\Eddy3D-Dev\Eddy3D-OutdoorIndoorTemplates`

You can also use `EDDY_TEMPLATE_REPO_DIR` (or `--repo-dir`) to override.
Use `--no-git-update` to skip fetch/pull (local branch must exist).
Use `--fix-counts` to apply guarded input/output count fixes:
- removes extra trailing ports
- adds missing ports only by cloning from a matching in-repo reference component (same GUID with expected counts)

## Visual Studio

1. Set `TemplateSync.Cli` as startup project.
2. Choose a launch profile in `Properties/launchSettings.json`.
3. Press Run/Debug.
