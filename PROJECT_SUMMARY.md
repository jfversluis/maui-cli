# MAUI CLI - Project Summary

## Overview

The MAUI CLI is a command-line tool designed to help .NET MAUI developers download and apply PR artifacts from the [dotnet/maui](https://github.com/dotnet/maui) repository. This tool is modeled after the [Aspire CLI](https://github.com/dotnet/aspire/tree/main/src/Aspire.Cli) and implements similar patterns for managing build artifacts.

## Project Status

✅ **Complete and Working**

- All builds succeed
- All tests pass (6/6 tests passing)
- Package can be built and distributed
- CI/CD pipeline configured

## Key Features Implemented

### 1. PR Artifact Discovery
- Connects to GitHub API to discover artifacts from pull requests
- Finds successful workflow runs associated with a PR
- Filters for NuGet package artifacts
- Handles authentication via GITHUB_TOKEN environment variable

### 2. Artifact Download and Management
- Downloads artifacts as ZIP files
- Extracts to local "hive" directories (`~/.maui/hives/pr-{number}`)
- Progress indicators during download
- Caching system for efficiency

### 3. Project Integration
- Automatically finds .csproj files in the current directory
- Updates Microsoft.Maui.* package references
- Creates/updates NuGet.config with local package sources
- Supports interactive project selection when multiple projects exist

### 4. User Experience
- Interactive CLI with Spectre.Console for rich output
- Clear error messages with actionable guidance
- Debug mode for troubleshooting
- Help documentation for all commands

## Architecture

### Core Components

```
Maui.Cli/
├── Commands/
│   └── ApplyPRCommand.cs       # Main command implementation
├── Services/
│   ├── GitHubArtifactService.cs   # GitHub API integration
│   ├── IGitHubArtifactService.cs
│   ├── NuGetService.cs            # NuGet operations
│   └── INuGetService.cs
├── Models/
│   └── PRArtifactInfo.cs         # Data models
├── CliExecutionContext.cs        # Execution context
├── ExitCodeConstants.cs          # Exit codes
└── Program.cs                    # Entry point and DI setup
```

### Key Design Patterns

1. **Dependency Injection**: Uses Microsoft.Extensions.DependencyInjection
2. **Separation of Concerns**: Services are split by responsibility
3. **Interface-based Design**: All services have interfaces for testability
4. **Command Pattern**: Commands are separate classes with clear responsibilities
5. **Hive System**: Mimics Aspire CLI's approach to managing artifact storage

## Testing

### Test Coverage

- **6 Unit Tests**: All passing
- **Test Coverage Areas**:
  - NuGetService functionality
  - CliExecutionContext construction
  - GitHubArtifactService API interactions
  - Error handling scenarios

### Test Framework

- xUnit for test execution
- Moq for mocking
- FluentAssertions for readable assertions
- Spectre.Console.Testing for console output verification

## CI/CD Pipeline

### GitHub Actions Workflow

Located at: `.github/workflows/build.yml`

**Build Job** (Multi-platform):
- Runs on: Ubuntu, Windows, macOS
- .NET 9.0 SDK
- Build, test, and code coverage

**Pack Job** (Ubuntu only):
- Creates NuGet package
- Uploads as artifact
- Only runs on main branch

## Usage Examples

### Basic Usage

```bash
# Set GitHub token for authentication
export GITHUB_TOKEN="your_token_here"

# Apply PR artifacts to current project
maui apply-pr 12345

# Apply to specific project
maui apply-pr 12345 --project MyApp/MyApp.csproj

# Enable debug output
maui apply-pr 12345 --debug
```

### Installation

```bash
# From NuGet package
dotnet tool install --global --add-source ./artifacts Maui.Cli

# Or from source
dotnet build
dotnet pack src/Maui.Cli/Maui.Cli.csproj
dotnet tool install --global --add-source ./artifacts Maui.Cli
```

## Build Instructions

### Prerequisites
- .NET 9.0 SDK or later
- Git

### Building

```bash
# Clone and build
git clone <repository-url>
cd maui-cli
dotnet restore
dotnet build

# Run tests
dotnet test

# Create package
dotnet pack src/Maui.Cli/Maui.Cli.csproj --output ./artifacts
```

### Running Locally

```bash
dotnet run --project src/Maui.Cli/Maui.Cli.csproj -- apply-pr 12345
```

## Technology Stack

- **Framework**: .NET 9.0
- **Command-line parsing**: System.CommandLine
- **UI**: Spectre.Console
- **HTTP**: HttpClient with Microsoft.Extensions.Http
- **Dependency Injection**: Microsoft.Extensions.Hosting
- **Testing**: xUnit, Moq, FluentAssertions
- **Build**: MSBuild, GitHub Actions

## Project Structure

```
maui-cli/
├── .github/
│   └── workflows/
│       └── build.yml              # CI/CD pipeline
├── src/
│   └── Maui.Cli/
│       ├── Commands/              # Command implementations
│       ├── Models/                # Data models
│       ├── Services/              # Core services
│       ├── Maui.Cli.csproj       # Project file
│       └── Program.cs             # Entry point
├── tests/
│   └── Maui.Cli.Tests/
│       ├── GitHubArtifactServiceTests.cs
│       ├── UnitTest1.cs
│       └── Maui.Cli.Tests.csproj
├── artifacts/                     # Build outputs
├── CHANGELOG.md                   # Version history
├── README.md                      # User documentation
├── PROJECT_SUMMARY.md            # This file
└── Maui.Cli.sln                  # Solution file
```

## Known Limitations and Future Enhancements

### Current Limitations
1. GitHub artifacts require authentication - users must set GITHUB_TOKEN
2. Only supports downloading from public dotnet/maui repository
3. Package version extraction from filename (could be improved with NuSpec parsing)

### Potential Future Enhancements
1. **List Command**: Show available PRs with builds
2. **Remove Command**: Remove installed hives
3. **Restore Command**: Restore original package versions
4. **Config Management**: Store GitHub token in secure configuration
5. **Multiple Framework Support**: Better handling of multi-targeted projects
6. **Automatic NuGet restore**: Run `dotnet restore` after updating packages
7. **Rollback Feature**: Ability to undo changes
8. **Cache Management**: Commands to manage the cache directory
9. **Better Error Recovery**: More detailed error messages and recovery suggestions

## Verification Checklist

- ✅ Solution builds successfully
- ✅ All tests pass (6/6)
- ✅ NuGet package builds
- ✅ CLI help works
- ✅ CI/CD pipeline configured
- ✅ Documentation complete
- ✅ MIT License included
- ✅ .gitignore configured
- ✅ README with examples
- ✅ CHANGELOG for versioning

## Contributing

The project is ready for contributions. Key areas for enhancement:
1. Additional commands (list, remove, restore)
2. More comprehensive test coverage
3. Integration tests with real GitHub API calls
4. Performance optimizations
5. Better progress reporting

## License

MIT License - See LICENSE file for details

## Acknowledgments

- Inspired by the [Aspire CLI](https://github.com/dotnet/aspire/tree/main/src/Aspire.Cli)
- Built for the .NET MAUI community
- Leverages Spectre.Console for beautiful terminal UI

---

**Project Completion Date**: November 13, 2025
**Version**: 1.0.0
**Status**: Production Ready ✅
