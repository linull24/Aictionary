# Aictionary Build Documentation

This project uses [NUKE](https://nuke.build/) for automated build and publishing.

## Prerequisites

- .NET 8.0 SDK or later
- NUKE global tool (will be installed automatically if missing)

## Available Build Targets

The build system supports the following targets:

- **Clean**: Cleans the artifacts directory
- **Restore**: Restores NuGet packages
- **Compile**: Compiles the solution
- **PublishWindows**: Publishes Windows x64 build
- **PublishMacOS**: Publishes macOS ARM64 build
- **Publish**: Publishes both Windows and macOS builds (default target)

## Quick Start

```bash
# From the project root directory
./build.sh

# Or on Windows
.\build.ps1
```

## How to Run

### Option 1: Using build scripts (Recommended)

#### On macOS/Linux:
```bash
./build.sh

# Publish only Windows
./build.sh --target PublishWindows

# Publish only macOS
./build.sh --target PublishMacOS
```

#### On Windows:
```powershell
.\build.ps1

# Publish only Windows
.\build.ps1 --target PublishWindows

# Publish only macOS
.\build.ps1 --target PublishMacOS
```

### Option 2: Using dotnet run

Navigate to the repository root and run:

```bash
# Publish all platforms (default)
dotnet run --project build/build.csproj

# Publish only Windows
dotnet run --project build/build.csproj --target PublishWindows

# Publish only macOS
dotnet run --project build/build.csproj --target PublishMacOS

# Just compile without publishing
dotnet run --project build/build.csproj --target Compile
```

## Output

Published artifacts will be located in the `artifacts` directory:

- **Windows**: `artifacts/windows/Aictionary-win-x64/`
- **macOS**: `artifacts/macos/Aictionary.app/`

The macOS build includes:
- Proper app bundle structure
- App icon (AppIcon.icns)
- Info.plist configuration

## Build Configuration

You can specify the configuration (Debug/Release) using the `--configuration` parameter:

```bash
dotnet run --project build/build.csproj --configuration Debug
```

Default configuration is **Release**.

## Notes

- The Windows build is self-contained and targets x64 architecture
- The macOS build is self-contained and targets ARM64 (Apple Silicon)
- Both builds use single-file publishing for easier distribution
- Trimming is disabled to avoid potential issues with reflection-heavy code

## Troubleshooting

If you encounter issues:

1. Ensure .NET 8.0 SDK is installed: `dotnet --version`
2. Clean and restore: `dotnet run --project build/build.csproj --target Clean`
3. Check the build output for specific error messages

For more information about NUKE, visit: https://nuke.build/
