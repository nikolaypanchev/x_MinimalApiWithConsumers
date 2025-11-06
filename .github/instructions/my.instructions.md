---
applyTo: "**"
---

# GitHub Copilot Instructions

## Coding style
- Use C# 12 features where appropriate.
- Follow .NET naming conventions (PascalCase for methods/classes, camelCase for variables).
- Always use `var` for local variables when the type is obvious.
- Prefer async/await over blocking calls.

## Architecture
- This solution follows Clean Architecture.
- Projects: `Application`, `Domain`, `Infrastructure`, `WebApi`.
- Keep business logic in the `Application` layer.
- Controllers should call Mediator handlers, not repositories directly.

## Comments and documentation
- When generating XML comments, include parameter and return descriptions.
- Keep comments concise and professional.

## Unit testing
- Use xUnit and FluentAssertions.
- Mock dependencies with Moq.
- When asked for tests, include both happy path and failure cases.

## Security and performance
- Never include hardcoded credentials or connection strings.
- Optimize LINQ queries to minimize database round-trips.
