# NewsSummarizer.Core.Tests

Unit tests for pure Core logic.

Covered areas:

- ArticleNormalizationService
- DeduplicationService
- RetentionPolicyService
- entity defaults

Run:

```powershell
dotnet test tests/NewsSummarizer.Core.Tests/NewsSummarizer.Core.Tests.csproj
```

This test project is intentionally not added to the main solution to reduce merge conflicts during active feature work.