# MAUI CLI

A command-line tool for .NET MAUI developers to:
- 🩺 **Check your development environment** - Verify all required tools and SDKs are installed
- 🚀 **Upgrade MAUI packages** - Update to stable or nightly builds (.NET 9/.NET 10)
- 📦 **Download and apply PR artifacts** - Test pull requests from [dotnet/maui](https://github.com/dotnet/maui) repository

## Features

- 🩺 **Environment Check**: Diagnose your .NET MAUI development environment
  - Verify .NET SDK version
  - Check MAUI workloads (Android, iOS, Windows, Mac Catalyst)
  - Validate platform-specific requirements (Java JDK, Android SDK, Xcode, Windows SDK)
  - Get actionable recommendations to fix issues
- 🚀 **Upgrade Command**: Update MAUI packages to latest versions
  - Upgrade to stable releases from NuGet.org
  - Switch to .NET 9 or .NET 10 nightly builds
  - Automatic NuGet feed configuration
  - Package source mapping setup
  - Detects current target framework and recommends appropriate channel
- 🔍 **Discover PR Artifacts**: Automatically finds build artifacts from pull requests
- 📦 **Download Packages**: Downloads NuGet packages from successful PR builds
- 🔄 **Apply to Projects**: Updates your MAUI project to use the PR artifacts
- 💾 **Local Hive Management**: Stores artifacts in a local "hive" for reuse

## Installation

### As a .NET Global Tool

```bash
dotnet tool install --global Maui.Cli
```

### From Source

```bash
git clone https://github.com/yourusername/maui-cli.git
cd maui-cli
dotnet build
dotnet pack src/Maui.Cli/Maui.Cli.csproj
dotnet tool install --global --add-source ./src/Maui.Cli/bin/Debug Maui.Cli
```

## Usage

### Authentication

To download artifacts from GitHub, you need to set a GitHub Personal Access Token:

```bash
# Windows (PowerShell)
$env:GITHUB_TOKEN = "your_github_token_here"

# Linux/macOS
export GITHUB_TOKEN="your_github_token_here"
```

Create a token at https://github.com/settings/tokens with `repo` scope.

### Check Your Environment

Diagnose your .NET MAUI development environment to ensure all required dependencies are installed and configured correctly. This command reads version requirements directly from the installed .NET SDK's `WorkloadDependencies.json` files, ensuring you have the exact versions needed for your installed workloads.

```bash
maui check
```

**What it checks:**
- ✅ **.NET SDK** - Version and runtime information
- ✅ **.NET MAUI Workloads** - Installed workloads for Android, iOS, macOS, Windows
- ✅ **Xcode** (macOS only) - Version requirements from iOS workload dependencies
- ✅ **Java JDK** - Version range requirements from Android workload dependencies (e.g., JDK 17-21)
- ✅ **Android SDK** - Required packages and build tools
- ✅ **Windows SDK** (Windows only) - Build tools and version

The check command uses the `WorkloadDependencies.json` files from your installed SDK manifests as the source of truth for version requirements, ensuring accurate compatibility checks.

**Options:**
- `--verbose` or `-v`: Show detailed diagnostic information including exact version requirements
- `--platform <platform>` or `-p <platform>`: Check specific platform (`android`, `ios`, `maccatalyst`, `windows`)
- `--manifest <url>` or `-m <url>`: Use a custom manifest file or URL for version requirements (fallback)

**Examples:**

```bash
# Check all applicable platforms for your OS
maui check

# Check only Android requirements
maui check --platform android

# Show verbose output with detailed version info and requirements
maui check --verbose

# Check iOS requirements on macOS
maui check --platform ios

# Use a custom manifest (fallback if SDK dependencies not found)
maui check --manifest https://example.com/custom-manifest.json
```

**Sample Output:**

```
────────────────────────────────────── Diagnostic Summary ──────────────────────────────────────

┌─────────────────────────┬────────┬─────────────────────────────────────────────────────────┐
│ Component               │ Status │ Details                                                 │
├─────────────────────────┼────────┼─────────────────────────────────────────────────────────┤
│ .NET SDK                │  √ OK  │ Version 10.0.100                                        │
│ MAUI Workload (Android) │  √ OK  │ Installed (version 36.1.2/10.0.100)                     │
│ MAUI Workload (Windows) │  √ OK  │ Installed (version 10.0.0/10.0.100)                     │
│ Java JDK                │  √ OK  │ Version 17 (JAVA_HOME: C:\Program Files\...)            │
│ Android SDK             │  √ OK  │ Found at C:\Program Files (x86)\Android\android-sdk     │
│ Windows SDK             │  √ OK  │ Windows 10.0 Build 26220                                │
└─────────────────────────┴────────┴─────────────────────────────────────────────────────────┘

√ All checks passed! Your environment is ready for .NET MAUI development.
```

### Upgrade MAUI Packages

Upgrade your .NET MAUI packages to the latest version:

```bash
maui upgrade
```

This command will:
1. Detect your project's current target framework (net9.0 or net10.0)
2. Present available upgrade channels:
   - **net9-stable** - Latest stable MAUI releases for .NET 9 from NuGet.org
   - **net10-stable** - Latest stable MAUI releases for .NET 10 from NuGet.org (with optional TFM upgrade)
   - **net9-nightly** - .NET 9 nightly builds from Azure DevOps
   - **net10-nightly** - .NET 10 nightly builds from Azure DevOps
3. Analyze your Microsoft.Maui.* packages
4. Show proposed updates in a table
5. Optionally upgrade target framework when switching to a different .NET version
6. Update package references and configure NuGet feeds

The tool intelligently filters package versions based on your target framework to ensure compatibility.

**Options:**
- `--project <path>`: Specify the project file to upgrade (auto-detected if not specified)
- `--channel <channel>`: Specify channel directly (`net9-stable`, `net10-stable`, `net9-nightly`, or `net10-nightly`)

**Examples:**

```bash
# Interactive upgrade (prompts for channel selection, recommends based on your TFM)
maui upgrade

# Upgrade to latest .NET 9 stable (filters to net9-compatible versions)
maui upgrade --channel net9-stable

# Upgrade to latest .NET 10 stable (will offer to upgrade TFM)
maui upgrade --channel net10-stable

# Upgrade a specific project to .NET 9 nightly builds
maui upgrade --project MyApp/MyApp.csproj --channel net9-nightly

# Upgrade to .NET 10 nightly
maui upgrade --channel net10-nightly
```

**Sample Output (Upgrading with TFM change):**

```
   __  __    _    _   _ ___   _   _                        _      
  |  \/  |  / \  | | | |_ _| | | | |_ __   __ _ _ __ __ _  | |  ___
  | |\/| | / _ \ | | | || |  | | | | '_ \ / _` | '__/ _` | | | / _ \
  | |  | |/ ___ \| |_| || |  | |_| | |_) | (_| | | | (_| | |_|  __/
  |_|  |_/_/   \_\\___/|___| \___/|_.__/ \__, |_|  \__,_| (_) \___|
                                         |___/

Select a channel to upgrade to:
  .NET 9 Stable (Latest MAUI for .NET 9) (recommended)
> .NET 10 Stable (Latest MAUI for .NET 10) + Upgrade TFM
  .NET 9 Nightly (Azure DevOps)
  .NET 10 Nightly (Azure DevOps)

Selected channel: .NET 10 Stable (Latest MAUI for .NET 10) + Upgrade TFM

Analyzing project: MyMauiApp.csproj
Current target framework: net9.0
Note: Selected channel targets net10.0, but project uses net9.0

Do you want to upgrade the project to net10.0? [y/n] (y): y
✓ Updated target framework to net10.0

Found 2 MAUI package(s)

┌─────────────────────────┬─────────────────┬─────────────┐
│ Package                 │ Current Version │ New Version │
├─────────────────────────┼─────────────────┼─────────────┤
│ Microsoft.Maui.Controls │ 9.0.120         │ 10.0.40     │
└─────────────────────────┴─────────────────┴─────────────┘

Apply these updates? [y/n] (y): y
Updating Microsoft.Maui.Controls...

✓ Upgrade completed successfully
Target framework updated to: net10.0
Updated packages:
  • Microsoft.Maui.Controls 9.0.120 → 10.0.40
```

### Apply PR Artifacts

Download and apply artifacts from a specific PR:

```bash
maui apply-pr 12345
```

This command will:
1. Fetch available artifacts from PR #12345
2. Let you select which artifact to download (if multiple are available)
3. Download and extract the artifact to `~/.maui/hives/pr-12345`
4. Find your .csproj file (or let you specify with `--project`)
5. Update package references to use the downloaded artifacts
6. Create/update NuGet.config to include the local package source

**Options:**
- `--project <path>` or `-p <path>`: Specify the project file to update
- `--debug` or `-d`: Enable debug output

**Examples:**

```bash
# Apply PR artifacts to a specific project
maui apply-pr 12345 --project MyApp/MyApp.csproj

# Enable debug output
maui apply-pr 12345 --debug
```

## How It Works

### Environment Check

The `maui check` command:
1. **Loads Manifest**: Downloads the latest requirement manifest from `https://aka.ms/dotnet-maui-check-manifest` (with embedded fallback)
2. **Runs Diagnostics**: Checks .NET SDK, workloads, and platform-specific tools
3. **Uses Native Tools**: Leverages `dotnet workload list --format json` for accurate workload detection
4. **Platform Detection**: Automatically checks only relevant tools for your OS
5. **Provides Recommendations**: Gives specific commands to fix issues (e.g., `dotnet workload install android`)

The manifest system allows remote updates without requiring tool updates, similar to [Redth's dotnet-maui-check](https://github.com/Redth/dotnet-maui-check).

### PR Artifacts

The MAUI CLI is modeled after the [Aspire CLI](https://github.com/dotnet/aspire/tree/main/src/Aspire.Cli) for PR artifact management:

1. **Hive System**: PR artifacts are stored in "hives" under `~/.maui/hives/pr-{number}`
2. **GitHub Integration**: Uses GitHub API to discover workflow runs and artifacts
3. **NuGet Integration**: Manages local NuGet sources and package updates
4. **Project Detection**: Automatically finds and updates .csproj files

### Directory Structure

```
~/.maui/
  ├── hives/          # Downloaded PR artifacts
  │   ├── pr-12345/   # Artifacts from PR #12345
  │   └── pr-67890/   # Artifacts from PR #67890
  └── cache/          # Cached metadata and manifest
```

## Development

### Prerequisites

- .NET 9.0 SDK or later
- Visual Studio 2022 or VS Code (optional)

### Building

```bash
dotnet build
```

### Running Tests

```bash
dotnet test
```

### Running Locally

```bash
# Run check command
dotnet run --project src/Maui.Cli/Maui.Cli.csproj -- check

# Run apply-pr command
dotnet run --project src/Maui.Cli/Maui.Cli.csproj -- apply-pr 12345
```

## Project Structure

```
maui-cli/
├── src/
│   └── Maui.Cli/
│       ├── Commands/          # Command implementations
│       ├── Models/            # Data models
│       ├── Services/          # Core services
│       └── Program.cs         # Entry point
├── tests/
│   └── Maui.Cli.Tests/        # Unit tests
└── .github/
    └── workflows/             # CI/CD pipelines
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License.

## Acknowledgments

- Inspired by the [Aspire CLI](https://github.com/dotnet/aspire/tree/main/src/Aspire.Cli) for PR artifact management
- Environment check inspired by [Redth's dotnet-maui-check](https://github.com/Redth/dotnet-maui-check)
- Built for the .NET MAUI community

## Compatibility

- ✅ Supports .NET 9 and .NET 10+ workload naming conventions
- ✅ Works on Windows, macOS, and Linux
- ✅ Platform-aware checks (only validates tools relevant to your OS)