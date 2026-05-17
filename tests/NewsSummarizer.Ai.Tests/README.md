# NewsSummarizer.Ai.Tests

Unit tests for AI integration logic.

Covered areas:

- AI response parsing
- JSON extraction from markdown / text
- score parsing and clamping
- parser fallbacks for MVP tolerance
- Yandex model URI construction
- Yandex config validation
- DI provider selection
- provider metadata
- prompt generation

Run:

```powershell
dotnet test tests/NewsSummarizer.Ai.Tests/NewsSummarizer.Ai.Tests.csproj
```