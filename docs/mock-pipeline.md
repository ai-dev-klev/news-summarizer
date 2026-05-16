# Mock pipeline

This step adds the first central pipeline on mock implementations.

## Added

- DatabaseSeeder
- FetchNewsUseCase
- AnalyzeArticleUseCase
- IAiProviderInfo
- Use case result records

## Current flow

```text
MockNewsFetcher
-> FetchNewsUseCase
-> NewsArticle with PendingAi status
-> MockAiProvider
-> AnalyzeArticleUseCase
-> ArticleAiResult
-> NewsArticle with Analyzed or Failed status
```

## Next step

Add debug endpoints:

```text
POST /debug/seed
POST /debug/fetch-news
POST /debug/analyze-articles
GET  /debug/articles/recent
```