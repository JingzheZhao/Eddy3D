<p align="center">
  <img src="https://raw.githubusercontent.com/Eddy3D-Dev/Eddy3D-Website/main/docs/assets/cd/LogoEddy-01_preview-crop.png" alt="Eddy3D full logo" width="280" />
</p>
<p align="center"><strong>Eddy3D</strong> - Airflow and Microclimate Simulations for Rhino & Grasshopper</p>

[![Build](https://img.shields.io/github/actions/workflow/status/Eddy3D-Dev/Eddy3D/build.yml?label=Build)](https://github.com/Eddy3D-Dev/Eddy3D/actions/workflows/build.yml)
[![Templates](https://img.shields.io/github/actions/workflow/status/Eddy3D-Dev/Eddy3D/template-branch-warning.yml?label=Templates)](https://github.com/Eddy3D-Dev/Eddy3D/actions/workflows/template-branch-warning.yml)
[![Version](https://img.shields.io/github/v/release/Eddy3D-Dev/Eddy3D?sort=semver)](https://github.com/Eddy3D-Dev/Eddy3D/releases)
[![Documentation](https://img.shields.io/badge/docs-eddy3d.com-blue)](https://docs.eddy3d.com)

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

## Testing

For xUnit test execution behavior across Windows/macOS (run/skip/fail-fast rules and required attributes), see:

- [`RhinoPlugin.Test.Xunit/TEST_POLICY.md`](RhinoPlugin.Test.Xunit/TEST_POLICY.md)

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
