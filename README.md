<p align="center">
  <img src="https://raw.githubusercontent.com/Eddy3D-Dev/Eddy3D-Website/main/docs/assets/cd/LogoEddy-01_preview-crop.png" alt="Eddy3D full logo" width="280" />
</p>
<p align="center"><strong>Eddy3D</strong> - Airflow and Microclimate Simulations for Rhino & Grasshopper</p>

[![Build](https://img.shields.io/github/actions/workflow/status/Eddy3D-Dev/Eddy3D/ci-build.yml?label=Build)](https://github.com/Eddy3D-Dev/Eddy3D/actions/workflows/ci-build.yml)
[![Templates](https://img.shields.io/github/actions/workflow/status/Eddy3D-Dev/Eddy3D/template-branch-warning.yml?label=Templates)](https://github.com/Eddy3D-Dev/Eddy3D/actions/workflows/template-branch-warning.yml)
[![Version](https://img.shields.io/github/v/release/Eddy3D-Dev/Eddy3D?sort=semver)](https://github.com/Eddy3D-Dev/Eddy3D/releases)
[![Yak](https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fyak.rhino3d.com%2Fpackages%2FEddy3D&query=%24.version&suffix=%20&logo=Rhinoceros&label=Yak)](https://rhinopackages.github.io/?search=Eddy3D&sort=2&p=Eddy3D)
[![Documentation](https://img.shields.io/badge/docs-eddy3d.com-blue)](https://docs.eddy3d.com)

## Release Branching

Eddy3D uses a promotion cascade for releases:

- `dev` is the active integration branch.
- `pre-release` is used to publish installer and Yak pre-releases for validation.
- `release` is used only for production releases.

The intended flow is `dev -> pre-release -> release`. Avoid merging feature branches directly into `release`. Since Yak package versions cannot be re-published, only promote to `pre-release` when the version is intentionally ready to publish.

## Development Setup (Windows)

<details>
<summary>Windows checkout steps</summary>

To check out the project for development on Windows, ensure you configure Git like this:

```powershell
# Create the parent directory if it doesn't exist
# This uses the current user's profile path
mkdir "$env:USERPROFILE\Documents\GitHub\Eddy3D-Dev" -ErrorAction SilentlyContinue
cd "$env:USERPROFILE\Documents\GitHub\Eddy3D-Dev"

# Clone the project
git clone https://github.com/Eddy3D-Dev/Eddy3D.git
cd Eddy3D
```

</details>



## Development Setup (macOS)

<details>
<summary>macOS checkout steps</summary>

To check out the project for development on macOS, run:

```bash
# Create the parent directory if it doesn't exist
mkdir -p "$HOME/Documents/GitHub/Eddy3D-Dev"
cd "$HOME/Documents/GitHub/Eddy3D-Dev"

# Clone the project
git clone https://github.com/Eddy3D-Dev/Eddy3D.git
cd Eddy3D
```

</details>

## Unit Tests & General Testing

<details>
<summary>Test policy</summary>

For xUnit test execution behavior across Windows/macOS (run/skip/fail-fast rules and required attributes), see:

- [`RhinoPlugin.Test.Xunit/TEST_POLICY.md`](RhinoPlugin.Test.Xunit/TEST_POLICY.md)

OpenFOAM execution tests are long-running integration tests and are skipped during normal unit test runs. To run them locally, set `EDDY3D_RUN_OPENFOAM_TESTS=1` and verify `EDDY3D_BLUECFD_DIR` points to the blueCFD-Core 2024 / OpenFOAM 12 installation. CI runs them through the separate [`OpenFOAM Integration Tests`](.github/workflows/openfoam-integration.yml) workflow, which loads defaults from [`ci/openfoam-integration.env`](ci/openfoam-integration.env).

</details>

