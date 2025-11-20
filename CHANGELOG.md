# Changelog

All notable changes to the MAUI CLI will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
