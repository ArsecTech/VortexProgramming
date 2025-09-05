---
name: Bug report
about: Create a report to help us improve Vortex Programming
title: '[BUG] '
labels: bug
assignees: ''
---

## 🐛 Bug Description

A clear and concise description of what the bug is.

## 🔄 To Reproduce

Steps to reproduce the behavior:
1. Create a VortexContext with '...'
2. Execute process '...'
3. See error

```csharp
// Minimal code example that reproduces the issue
var context = VortexContext.ForDevelopment("tenant");
var process = new MyProcess();
var result = await process.ExecuteAsync(context, input);
```

## ✅ Expected Behavior

A clear and concise description of what you expected to happen.

## ❌ Actual Behavior

A clear and concise description of what actually happened.

## 🖥️ Environment

- **OS**: [e.g. Windows 11, Ubuntu 22.04, macOS 14]
- **Vortex Programming Version**: [e.g. 1.0.0]
- **.NET Version**: [e.g. .NET 9.0]
- **IDE**: [e.g. Visual Studio 2022, VS Code, Rider]

## 📋 Additional Context

- **VortexContext Configuration**: [e.g. Development, Production, specific properties]
- **Process Type**: [e.g. VortexProcess, Process Chain, Fluent API]
- **Scale**: [e.g. Small, Medium, Large, Auto]
- **Tenant Configuration**: [e.g. single tenant, multi-tenant]

## 📄 Logs/Stack Trace

```
Paste any relevant log output or stack traces here
```

## 🔍 Screenshots

If applicable, add screenshots to help explain your problem.

## 🤔 Possible Solution

If you have ideas on how to fix this, please describe them here.

## 📚 Related Issues

Link any related issues: #issue_number 