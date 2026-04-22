# Contributing to JellyRequest

Thank you for your interest in contributing to JellyRequest! This document provides guidelines for contributing to this project.

## Development Setup

### Prerequisites
- .NET 9.0 SDK
- Jellyfin 10.11.8.0 or higher
- Docker (for testing)

### Building
```bash
# Clone the repository
git clone https://github.com/yourusername/Jellyfin.Plugin.JellyRequest.git
cd Jellyfin.Plugin.JellyRequest

# Restore dependencies
dotnet restore

# Build the project
dotnet build -c Release
```

### Testing
```bash
# Run tests (when implemented)
dotnet test

# For manual testing in Docker
docker build -t jellyrequest-test .
docker run -p 8096:8096 jellyrequest-test
```

## Code Style

- Follow C# conventions
- Use meaningful variable and method names
- Add XML documentation for public APIs
- Keep methods focused and small

## Submitting Changes

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## Bug Reports

When filing bug reports, please include:
- Jellyfin version
- Plugin version
- Steps to reproduce
- Expected vs actual behavior
- Relevant logs

## Feature Requests

Feature requests are welcome! Please open an issue with:
- Clear description of the feature
- Use case scenario
- Any implementation ideas

## License

By contributing, you agree that your contributions will be licensed under the same license as the project.
