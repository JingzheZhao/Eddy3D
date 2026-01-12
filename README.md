[![Build Eddy3D](https://github.com/Eddy3D-Dev/Eddy3D/actions/workflows/build.yml/badge.svg)](https://github.com/Eddy3D-Dev/Eddy3D/actions/workflows/build.yml)

## Development Setup (Windows)

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
