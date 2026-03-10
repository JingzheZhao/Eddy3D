# Eddy3D Agent Guide

This file defines the working conventions for automated agents editing this repository.

## Repository Map

- `EddyLib/`: shared simulation, geometry, Docker, probing, OpenFOAM, and utility code. Targets `net8.0`.
- `Eddy/`: Rhino/Grasshopper plugin project. Targets `net8.0-windows`.
- `RhinoPlugin.Test.Xunit/`: xUnit test suite with OS-aware Rhino host policy.
- `TemplateSync.Cli/`: CLI for validating and syncing Grasshopper template labels.
- `TemplatesInternal/`: internal template and fixture assets.
- `Build/`: installer/build support scripts, primarily Windows-oriented.

## Platform Notes

- This repo is cross-platform for some workflows, but not all of them.
- `EddyLib` and `TemplateSync.Cli` are safe first targets for local validation on macOS.
- `Eddy` is Windows-targeted, but `dotnet build Eddy/Eddy.csproj` can still compile on macOS with the current project settings.
- Rhino-dependent tests are governed by `RhinoPlugin.Test.Xunit/TEST_POLICY.md`.
- On macOS, Rhino tests can run if Rhino is installed under `/Applications/Rhino 8.app`, `/Applications/Rhino.app`, or `/Applications/RhinoWIP.app`.
- Docker-dependent tests fail when Docker Desktop is installed but the daemon/socket is not running. Do not treat those failures as code regressions without checking Docker availability first.

## Preferred Validation Commands

Run the narrowest command that proves the change.

```bash
dotnet build EddyLib/EddyLib.csproj -v minimal
dotnet build TemplateSync.Cli/TemplateSync.Cli.csproj -v minimal
dotnet build Eddy/Eddy.csproj -v minimal
dotnet test RhinoPlugin.Test.Xunit/RhinoPlugin.Test.Xunit.csproj -v minimal
```

Template sync CLI examples:

```bash
dotnet run --project TemplateSync.Cli -- --repo-dir /path/to/Eddy3D-OutdoorIndoorTemplates
dotnet run --project TemplateSync.Cli -- --repo-dir /path/to/Eddy3D-OutdoorIndoorTemplates --fix
```

## Current Local Baseline

Verified on macOS on March 10, 2026:

- `dotnet build EddyLib/EddyLib.csproj` succeeds.
- `dotnet build TemplateSync.Cli/TemplateSync.Cli.csproj` succeeds.
- `dotnet build Eddy/Eddy.csproj` succeeds.
- `dotnet test RhinoPlugin.Test.Xunit/RhinoPlugin.Test.Xunit.csproj` runs with most Rhino-related tests skipped by policy when requirements are unavailable.
- Current known local test failures are Docker-related in `RhinoPlugin.Test.Xunit/Tests/Test.Docker.cs` when the Docker daemon socket is unavailable.

## Editing Rules

- Prefer changing `EddyLib` first when functionality is shared between the plugin, CLI, and tests.
- Keep test policy centralized through `RhinoPlugin.Test.Xunit/TestExecutionPolicy.cs`; do not add ad hoc OS checks in individual tests unless necessary.
- Avoid editing generated or build output directories such as `bin/`, `obj/`, and `packages/`.
- Preserve existing project targeting and post-build behaviors unless the task explicitly requires changing them.
- Be careful with Rhino, Grasshopper, and Docker path logic. Those code paths are platform-sensitive.

## Versioning Convention

- The Eddy3D version number follows `Major.Minor.Patch.RhinoVersion`.
- The final segment indicates the Rhino version the release was tested with.
- Example: `0.5.8.815` means tested with Rhino `8.15`.
- If updating release/version documentation, ensure any "tested with Rhino" text matches the suffix.

## Markdown and Changelog Styling

When editing changelog-style release notes:

- Category headings must use this form:
  - `- Added:`
  - `- Improved:`
  - `- Fixed:`
- Entries under each category should be indented by two spaces.
- Those entries should not use bullets.
- Separate entries with a blank line.
- Remove stray encoding artifacts such as `Â` if they appear.

## CI Context

- GitHub Actions build runs on `windows-latest`; see `.github/workflows/build.yml`.
- CI currently restores with NuGet and builds the solution with MSBuild.
- Historically, automated tests in CI have been limited by Rhino runtime availability.

## When in Doubt

- Inspect the specific `.csproj` before changing build logic.
- Validate the smallest affected project instead of the whole solution.
- If a failure mentions Rhino runtime discovery or Docker connectivity, check environment assumptions before changing code.
