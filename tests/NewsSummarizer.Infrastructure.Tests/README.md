# NewsSummarizer.Infrastructure.Tests

Integration tests for PostgreSQL persistence.

These tests use Testcontainers and start a temporary PostgreSQL container.

Run:

```powershell
dotnet test tests/NewsSummarizer.Infrastructure.Tests/NewsSummarizer.Infrastructure.Tests.csproj
```

Requirements:

- Docker Desktop or Docker Engine must be running.
- Tests are intentionally not added to `NewsSummarizer.sln` to reduce merge conflicts during active development.

Covered areas:

- EF Core migrations
- expected tables and snake_case columns
- repository behavior
- unique constraints
- check constraints
- foreign key delete behavior
- database seeding idempotency
- expired data cleanup