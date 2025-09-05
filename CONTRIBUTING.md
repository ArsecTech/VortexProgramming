# Contributing to Vortex Programming Framework

We love your input! We want to make contributing to Vortex Programming as easy and transparent as possible, whether it's:

- Reporting a bug
- Discussing the current state of the code
- Submitting a fix
- Proposing new features
- Becoming a maintainer

## We Develop with Github

We use GitHub to host code, to track issues and feature requests, as well as accept pull requests.

## We Use [Github Flow](https://guides.github.com/introduction/flow/index.html)

Pull requests are the best way to propose changes to the codebase. We actively welcome your pull requests:

1. Fork the repo and create your branch from `main`.
2. If you've added code that should be tested, add tests.
3. If you've changed APIs, update the documentation.
4. Ensure the test suite passes.
5. Make sure your code lints.
6. Issue that pull request!

## Any contributions you make will be under the MIT Software License

In short, when you submit code changes, your submissions are understood to be under the same [MIT License](http://choosealicense.com/licenses/mit/) that covers the project. Feel free to contact the maintainers if that's a concern.

## Report bugs using Github's [issues](https://github.com/your-org/VortexProgramming/issues)

We use GitHub issues to track public bugs. Report a bug by [opening a new issue](https://github.com/your-org/VortexProgramming/issues/new); it's that easy!

## Write bug reports with detail, background, and sample code

**Great Bug Reports** tend to have:

- A quick summary and/or background
- Steps to reproduce
  - Be specific!
  - Give sample code if you can
- What you expected would happen
- What actually happens
- Notes (possibly including why you think this might be happening, or stuff you tried that didn't work)

## Development Setup

1. Clone the repository
```bash
git clone https://github.com/your-org/VortexProgramming.git
cd VortexProgramming
```

2. Install .NET 9 SDK
- Download from [Microsoft .NET](https://dotnet.microsoft.com/download/dotnet/9.0)

3. Restore dependencies
```bash
dotnet restore
```

4. Build the solution
```bash
dotnet build
```

5. Run tests
```bash
dotnet test
```

6. Run the demo
```bash
cd demo/VortexProgramming.Demo
dotnet run
```

## Code Style

- Use C# 13 features where appropriate
- Follow Microsoft's C# coding conventions
- Use meaningful names for variables, methods, and classes
- Write XML documentation for public APIs
- Keep methods focused and concise
- Use async/await for asynchronous operations

## Testing Guidelines

- Write unit tests for all public APIs
- Use descriptive test method names
- Follow AAA pattern (Arrange, Act, Assert)
- Mock external dependencies
- Aim for high test coverage

## Documentation

- Update README.md if you change functionality
- Add XML documentation to public classes and methods
- Include code examples for new features
- Update CHANGELOG.md for significant changes

## Pull Request Process

1. Update the README.md with details of changes if applicable
2. Update the version numbers in any examples files and the README.md to the new version that this Pull Request would represent
3. Ensure any install or build dependencies are removed before the end of the layer when doing a build
4. You may merge the Pull Request in once you have the sign-off of two other developers, or if you do not have permission to do that, you may request the second reviewer to merge it for you

## Code of Conduct

### Our Pledge

In the interest of fostering an open and welcoming environment, we as contributors and maintainers pledge to making participation in our project and our community a harassment-free experience for everyone.

### Our Standards

Examples of behavior that contributes to creating a positive environment include:

- Using welcoming and inclusive language
- Being respectful of differing viewpoints and experiences
- Gracefully accepting constructive criticism
- Focusing on what is best for the community
- Showing empathy towards other community members

### Enforcement

Instances of abusive, harassing, or otherwise unacceptable behavior may be reported by contacting the project team. All complaints will be reviewed and investigated and will result in a response that is deemed necessary and appropriate to the circumstances.

## License

By contributing, you agree that your contributions will be licensed under its MIT License. 