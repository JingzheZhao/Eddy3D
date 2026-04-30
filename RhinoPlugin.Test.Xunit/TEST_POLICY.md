# Test Execution Policy

This document defines the test execution contract for `RhinoPlugin.Test.Xunit`.

## Goals

- Keep behavior predictable across platforms.
- Avoid silent skips on Windows 11 desktop when Rhino host startup is broken.
- Use a single source of truth for run/skip decisions.

## Source of Truth

Policy is centralized in:

- `RhinoPlugin.Test.Xunit/TestExecutionPolicy.cs`

All custom test attributes must route through `TestExecutionPolicy`.

## Policy Matrix

| Requirement | Windows 11 Desktop | Windows Server CI | macOS |
|---|---|---|---|
| `RhinoInstalled` | Run (do not skip by policy) | Skip | Run if Rhino installed, else Skip |
| `RhinoNativeHost` | Run | Skip | Skip |
| `GrasshopperWithRhinoHost` | Run if Grasshopper available, else Skip | Skip | Skip |
| `BlueCfd` | Run if blueCFD-Core 2024 / OpenFOAM 12 is available | Skip | Skip |
| `OpenFoamExecution` | Run only when `EDDY3D_RUN_OPENFOAM_TESTS=1` | Skip | Skip |
| `Radiance` | Run if Radiance is available | Skip | Skip |
| `EnergyPlus` | Run if EnergyPlus is available | Skip | Skip |
| `Python` | Run if `python` or `python3` starts successfully | Run if available | Run if available |
| `ExternalService` | Run only when `EDDY3D_RUN_EXTERNAL_TESTS=1` | Run only when enabled | Run only when enabled |

### Fail-Fast Rule

On Windows desktop (non-server), Rhino native host initialization failures are treated as failures, not skips.

- Implemented by `TestExecutionPolicy.ShouldFailFastOnRhinoHostInitialization()`
- Enforced in `XunitTestInitFixture`

## Attribute Mapping

Use these attributes in tests:

- `RhinoRequiredFact` / `RhinoRequiredTheory`
  - Maps to `RhinoInstalled`
- `NotWindowsServerFact` / `NotWindowsServerTheory`
  - Backward-compatible names; maps to `RhinoNativeHost`
- `RequiresGrasshopperFact` / `RequiresGrasshopperTheory`
  - Maps to `GrasshopperWithRhinoHost`
- `RequiresOpenFoamExecutionFact` / `RequiresOpenFoamExecutionTheory`
  - Maps to `RhinoNativeHost`, `BlueCfd`, and `OpenFoamExecution`
- `RequiresRadianceFact` / `RequiresRadianceTheory`
  - Maps to `RhinoNativeHost` and `Radiance`
- `RequiresRadianceAndEnergyPlusFact`
  - Maps to `RhinoNativeHost`, `Radiance`, and `EnergyPlus`
- `RequiresPythonFact`
  - Maps to `Python`
- `RequiresExternalServiceFact`
  - Maps to `ExternalService`

## OpenFOAM Integration Tests

OpenFOAM execution tests are long-running integration tests, not default unit tests.

Local/manual execution:

```powershell
$env:EDDY3D_RUN_OPENFOAM_TESTS = "1"
$env:EDDY3D_BLUECFD_DIR = "C:\Program Files\blueCFD-Core-2024"
$env:EDDY3D_TEST_CPUS = "2"
dotnet test RhinoPlugin.Test.Xunit\RhinoPlugin.Test.Xunit.csproj --filter "FullyQualifiedName~OFExecutionTests"
```

CI execution is handled by `.github/workflows/openfoam-integration.yml`. The workflow loads `ci/openfoam-integration.env` and targets a self-hosted Windows runner with the `OpenFOAM` label. That runner must have Rhino and blueCFD-Core 2024 / OpenFOAM 12 installed.

Environment variables:

- `EDDY3D_RUN_OPENFOAM_TESTS=1`: enables the slow execution tests.
- `EDDY3D_BLUECFD_DIR`: overrides blueCFD auto-detection.
- `EDDY3D_TEST_CPUS`: caps solver CPU count for constrained runners.

## Fixture Behavior

`XunitTestInitFixture`:

1. Applies policy checks before host startup.
2. Skips where policy requires skip.
3. Starts Rhino host only when policy allows.
4. Fails fast on Windows desktop host initialization errors.

## Adding New Tests

1. Decide the minimum requirement:
   - Rhino install only
   - Rhino native host
   - Grasshopper + Rhino native host
2. Use the matching attribute.
3. Do not add ad-hoc OS checks inside tests unless there is a strong case.
4. If a new requirement type is needed, add it to `TestExecutionRequirement` and implement it in `TestExecutionPolicy`.

## Notes

- Attribute names are intentionally backward-compatible to avoid broad test code churn.
- `NotWindowsServer*` currently means "requires Rhino native host", not only "skip on server".
