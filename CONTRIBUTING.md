# Contributing to MAUI CLI

Thank you for your interest in contributing to MAUI CLI! This document provides guidelines and instructions for contributing.

## Getting Started

### Prerequisites

- .NET 9.0 SDK or later
- Git
- A GitHub account

### Setup Development Environment

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/maui-cli.git
   cd maui-cli
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Build the project**
   ```bash
   dotnet build
   ```

4. **Run tests**
   ```bash
   dotnet test
   ```

5. **Run the tool locally**
   ```bash
   # Check command
   dotnet run --project src/Maui.Cli/Maui.Cli.csproj -- check
   
   # Upgrade command
   dotnet run --project src/Maui.Cli/Maui.Cli.csproj -- upgrade
   
   # Apply PR command
   dotnet run --project src/Maui.Cli/Maui.Cli.csproj -- apply-pr 12345
   ```

## Project Structure

```
maui-cli/
├── src/
│   └── Maui.Cli/
│       ├── Commands/          # Command implementations (CheckCommand, ApplyPrCommand, etc.)
│       ├── Models/            # Data models (CheckResult, CheckManifest, etc.)
│       ├── Services/          # Core services (EnvironmentCheckService, ManifestService, etc.)
│       ├── default-manifest.json  # Embedded fallback manifest
│       └── Program.cs         # Entry point with dependency injection
├── tests/
│   └── Maui.Cli.Tests/        # Unit tests
│       ├── Commands/          # Command tests
│       ├── Services/          # Service tests
│       └── ...
└── .github/
    └── workflows/             # CI/CD pipelines
```

## Development Guidelines

### Code Style

- Follow standard C# conventions
- Use meaningful variable and method names
- Keep methods focused and single-purpose
- Add XML comments for public APIs
- Use `async`/`await` for I/O operations

### Adding a New Command

1. Create a new class in `src/Maui.Cli/Commands/` inheriting from `Command`
2. Implement the command logic
3. Register the command in `Program.cs`
4. Add tests in `tests/Maui.Cli.Tests/Commands/`
5. Update `README.md` with usage examples

Example:
```csharp
internal sealed class MyCommand : Command
{
    private readonly IMyService _myService;

    public MyCommand(IMyService myService) : base("my-command", "Description")
    {
        _myService = myService;
        
        var myOption = new Option<string>("--my-option", "Option description");
        AddOption(myOption);
        
        this.SetHandler(ExecuteAsync, myOption);
    }

    private async Task<int> ExecuteAsync(string myOption)
    {
        // Implementation
        return 0;
    }
}
```

### Adding a New Service

1. Create an interface in `src/Maui.Cli/Services/` (e.g., `IMyService`)
2. Implement the interface
3. Register in `Program.cs` dependency injection
4. Add unit tests with mocked dependencies
5. Document public methods with XML comments

### Testing

- Write unit tests for all new features
- Aim for high code coverage
- Use FluentAssertions for readable assertions
- Mock external dependencies with Moq
- Test both success and failure paths

Example test:
```csharp
[Fact]
public async Task MyMethod_ShouldReturnExpectedResult()
{
    // Arrange
    var mockService = new Mock<IMyService>();
    mockService.Setup(s => s.DoSomething()).ReturnsAsync(expectedResult);
    
    // Act
    var result = await mockService.Object.DoSomething();
    
    // Assert
    result.Should().Be(expectedResult);
}
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test
dotnet test --filter "FullyQualifiedName~CheckCommand"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Pull Request Process

1. **Fork the repository** and create a new branch from `main`
   ```bash
   git checkout -b feature/my-new-feature
   ```

2. **Make your changes**
   - Write clean, maintainable code
   - Follow the code style guidelines
   - Add tests for new functionality
   - Update documentation as needed

3. **Commit your changes**
   ```bash
   git add .
   git commit -m "Add feature: description of changes"
   ```
   
   Use clear, descriptive commit messages:
   - `Add feature: ...`
   - `Fix bug: ...`
   - `Update docs: ...`
   - `Refactor: ...`

4. **Push to your fork**
   ```bash
   git push origin feature/my-new-feature
   ```

5. **Open a Pull Request**
   - Provide a clear description of the changes
   - Link any related issues
   - Ensure all CI checks pass
   - Request review from maintainers

### Pull Request Checklist

- [ ] Code builds without errors
- [ ] All tests pass
- [ ] New tests added for new functionality
- [ ] Documentation updated (README.md, CHANGELOG.md)
- [ ] Code follows project conventions
- [ ] Commit messages are clear and descriptive

## Updating the Manifest

The `maui check` command uses a remote manifest to determine version requirements. The manifest is hosted at `https://aka.ms/dotnet-maui-check-manifest`.

### Manifest Structure

```json
{
  "check": {
    "variables": {
      "MIN_ANDROID_API": "21",
      "TARGET_ANDROID_API": "34"
    },
    "openjdk": {
      "version": "17.0",
      "minimumVersion": "11.0"
    },
    "xcode": {
      "minimumVersion": "15",
      "minimumVersionName": "15.0"
    },
    "androidSdk": {
      "packages": [
        {
          "path": "platforms;android-34",
          "version": "34"
        }
      ]
    },
    "dotnetWorkloads": [
      "android",
      "ios",
      "maccatalyst",
      "maui-windows"
    ]
  }
}
```

To update requirements:
1. Modify the embedded manifest at `src/Maui.Cli/default-manifest.json`
2. Submit a PR to update the hosted manifest URL

## Reporting Issues

- Use GitHub Issues to report bugs
- Provide clear reproduction steps
- Include relevant logs and environment details
- Check if the issue already exists before creating a new one

### Bug Report Template

```
**Description**
A clear description of the bug

**Steps to Reproduce**
1. Run command `maui check`
2. See error...

**Expected Behavior**
What you expected to happen

**Actual Behavior**
What actually happened

**Environment**
- OS: Windows/macOS/Linux
- .NET SDK Version: 
- MAUI CLI Version:
```

## Feature Requests

- Open a GitHub Issue with the `enhancement` label
- Describe the feature and its benefits
- Provide use cases and examples
- Be open to discussion and feedback

## Questions?

Feel free to open a GitHub Discussion or Issue if you have questions about contributing.

## Code of Conduct

- Be respectful and inclusive
- Welcome newcomers
- Focus on constructive feedback
- Help maintain a positive community

## License

By contributing to MAUI CLI, you agree that your contributions will be licensed under the MIT License.

---

Thank you for contributing to MAUI CLI! 🎉
