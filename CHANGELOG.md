# Changelog

All notable changes to the MAUI CLI will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- **`maui upgrade` command** - Upgrade .NET MAUI packages to latest versions
  - Support for stable channel (NuGet.org)
  - Support for .NET 9 nightly builds from Azure DevOps
  - Support for .NET 10 nightly builds from Azure DevOps
  - Automatic target framework detection
  - Channel recommendation based on current project
  - NuGet feed configuration with package source mappings
  - Interactive channel selection or command-line specification
  - Beautiful table output showing proposed updates
  - XML-based and CLI-based package update strategies
- **`maui check` command** - Comprehensive .NET MAUI environment diagnostics
  - Verifies .NET SDK installation and version
  - Checks MAUI workloads for all platforms (Android, iOS, Windows, Mac Catalyst)
  - Validates platform-specific requirements:
    - Java JDK for Android development
    - Android SDK location and version
    - Xcode for iOS/Mac development (macOS only)
    - Windows SDK for Windows development
  - Provides actionable recommendations with fix commands
  - Supports both .NET 9 and .NET 10+ workload naming conventions
  - Remote manifest system for updateable requirements (inspired by dotnet-maui-check)
  - Platform filtering with `--platform` flag
  - Verbose mode with `--verbose` flag
  - Custom manifest URLs with `--manifest` flag
  - Beautiful table output with status indicators
- **Manifest Service** - Downloads and caches requirements manifest from `https://aka.ms/dotnet-maui-check-manifest`
- **Embedded fallback manifest** - Works offline with default requirements
- **JSON-based workload detection** - Uses `dotnet workload list --format json` for accurate detection
- Comprehensive test suite for check command (41 tests covering all scenarios)

### Fixed
- .NET 10+ workload naming compatibility (now checks for both `android` and `maui-android` conventions)
- Moq compatibility with internal interfaces (added `DynamicProxyGenAssembly2` to `InternalsVisibleTo`)

## [1.0.0] - 2025-11-13

### Added
- Initial release of MAUI CLI
- `apply-pr` command to download and apply PR artifacts from dotnet/maui repository
- GitHub API integration to discover PR artifacts
- Automatic project detection and package reference updates
- Local "hive" system for storing downloaded artifacts
- NuGet.config management for local package sources
- Support for GitHub Personal Access Token authentication via GITHUB_TOKEN environment variable
- Comprehensive test suite with unit and integration tests
- CI/CD pipeline with GitHub Actions
- Cross-platform support (Windows, Linux, macOS)

### Features
- Download NuGet packages from successful PR builds
- Automatically update MAUI package references in .csproj files
- Interactive artifact selection when multiple artifacts are available
- Progress indicators for download operations
- Debug mode for troubleshooting
- Clean error messages with actionable guidance

### Documentation
- Comprehensive README with installation and usage instructions
- Examples and best practices
- Architecture documentation inspired by Aspire CLI
