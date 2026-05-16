# Provider selection

The project supports configurable providers for local development and MVP demo.

## Fetching provider

Configuration:

```json
"NewsFetching": {
  "Provider": "Mock"
}
```

Environment variable:

```text
NEWS_FETCHING_PROVIDER=Mock
```

Supported values:

```text
Mock
Rss
```

Current default:

```text
Mock
```

## AI provider

Configuration:

```json
"Ai": {
  "Provider": "Mock"
}
```

Environment variable:

```text
AI_PROVIDER=Mock
```

Supported values:

```text
Mock
Yandex
```

Current default:

```text
Mock
```

## Test modes

```text
Mock fetching + Mock AI
Rss fetching + Mock AI
Mock fetching + Yandex AI
Rss fetching + Yandex AI
```

## Recommended local default

```text
NEWS_FETCHING_PROVIDER=Mock
AI_PROVIDER=Mock
```

Use real providers only after their implementations are merged and configured.