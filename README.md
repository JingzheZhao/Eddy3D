[![Build Eddy3D](https://github.com/Eddy3D-Dev/Eddy3D/actions/workflows/build.yml/badge.svg)](https://github.com/Eddy3D-Dev/Eddy3D/actions/workflows/build.yml)

## Development Setup (Windows)

To check out the project for development on Windows, ensure you configure Git to handle line endings correctly.

1.  **Configure Git Line Endings**:
    Run the following command to ensure `core.autocrlf` is set to `true`. This converts LF to CRLF when checking out text files, which is standard for Windows development.
    ```powershell
    git config --global core.autocrlf true
    ```

2.  **Clone the Repository**:
    ```powershell
    git clone https://github.com/Eddy3D-Dev/Eddy3D.git
    cd Eddy3D
    ```
