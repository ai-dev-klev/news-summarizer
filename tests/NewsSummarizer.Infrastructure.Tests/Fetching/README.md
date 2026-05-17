# Fetching tests

Unit tests for infrastructure fetchers.

Covered areas:

- RSS 2.0 parsing
- Atom parsing
- skipping invalid RSS/Atom items
- date parsing and UTC normalization
- missing/invalid dates
- HTTP failure handling
- malformed XML handling
- source type guard for RSS and Mock fetchers
- Mock fetcher MVP scenario data

Run only infrastructure tests:

```powershell
dotnet test tests/NewsSummarizer.Infrastructure.Tests/NewsSummarizer.Infrastructure.Tests.csproj
```

These fetcher tests do not use external network calls.