# MAUI CLI

A command-line tool for .NET MAUI developers to download and apply PR artifacts from the [dotnet/maui](https://github.com/dotnet/maui) repository.

## Features

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

### Options

- `--project <path>` or `-p <path>`: Specify the project file to update
- `--debug` or `-d`: Enable debug output

### Example

```bash
# Apply PR artifacts to a specific project
maui apply-pr 12345 --project MyApp/MyApp.csproj

# Enable debug output
maui apply-pr 12345 --debug
```

## How It Works

### Architecture

The MAUI CLI is modeled after the [Aspire CLI](https://github.com/dotnet/aspire/tree/main/src/Aspire.Cli) and follows similar patterns:

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
  └── cache/          # Cached metadata
```

## Development

### Prerequisites

- .NET 9.0 SDK or later
- Visual Studio 2022 or VS Code

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

- Inspired by the [Aspire CLI](https://github.com/dotnet/aspire/tree/main/src/Aspire.Cli)
- Built for the .NET MAUI community