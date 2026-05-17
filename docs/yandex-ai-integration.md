# Yandex AI Studio integration

## Что делает модуль

AI-модуль преобразует новость в структурированную оценку:

```text
NewsArticle -> YandexAiProvider -> ArticleAiAnalysisResult
```

Результат используется дальше для отбора новостей в дайджесты и уведомления.

## Основной API

Для MVP используется OpenAI-compatible Chat Completions API Yandex AI Studio через пакет `OpenAI` для .NET.

MCP Gateway не входит в MVP-ядро. Он нужен позже для агентных сценариев с tools.

## Настройки

Основная секция конфигурации:

```json
{
  "AiProvider": {
    "Provider": "Yandex",
    "BaseUrl": "https://ai.api.cloud.yandex.net/v1",
    "ApiKey": "",
    "FolderId": "",
    "Model": "yandexgpt/rc",
    "PromptVersion": "v1",
    "MaxOutputTokens": 800,
    "Temperature": 0.2,
    "RequestTimeoutSeconds": 60
  }
}
```

Также поддерживается старая секция `Ai`.

Env-переменные имеют приоритет над значениями из конфигурации:

```text
YANDEX_AI_API_KEY
YANDEX_AI_FOLDER_ID
YANDEX_AI_BASE_URL
YANDEX_AI_MODEL
YANDEX_AI_PROMPT_VERSION
YANDEX_AI_PROVIDER
```

Короткие варианты тоже поддерживаются:

```text
YANDEX_API_KEY
YANDEX_FOLDER_ID
YANDEX_BASE_URL
YANDEX_MODEL
YANDEX_PROMPT_VERSION
AI_PROVIDER
```

## Локальная настройка через user-secrets

```bash
dotnet user-secrets init --project src/NewsSummarizer.Api

dotnet user-secrets set "AiProvider:Provider" "Yandex" --project src/NewsSummarizer.Api
dotnet user-secrets set "AiProvider:ApiKey" "<YANDEX_API_KEY>" --project src/NewsSummarizer.Api
dotnet user-secrets set "AiProvider:FolderId" "<YANDEX_FOLDER_ID>" --project src/NewsSummarizer.Api
dotnet user-secrets set "AiProvider:BaseUrl" "https://ai.api.cloud.yandex.net/v1" --project src/NewsSummarizer.Api
dotnet user-secrets set "AiProvider:Model" "yandexgpt/rc" --project src/NewsSummarizer.Api
```

## NuGet

Нужен пакет:

```bash
dotnet add src/NewsSummarizer.Ai/NewsSummarizer.Ai.csproj package OpenAI
```

## Что возвращает AnalyzeArticleAsync

Ожидаемый JSON от модели:

```json
{
  "category": "technology",
  "importanceScore": 80,
  "urgencyScore": 20,
  "opportunityScore": 70,
  "summary": "Short summary",
  "reason": "Why it matters",
  "opportunityReason": "Why it may be useful for analysis",
  "dailyDigestCandidate": true,
  "opportunityDigestCandidate": true,
  "urgentCandidate": false
}
```

## Fallback без structured output

`YandexAiProvider` сначала пробует `ResponseFormat = json_schema`.
Если совместимый endpoint вернёт `400` из-за неподдерживаемого `response_format/json_schema`, провайдер повторит запрос без `ResponseFormat`, но с жёсткой JSON-инструкцией в prompt.

## Проверка

Минимально:

```bash
dotnet build
```

Потом проверить три новости:

```text
1. обычная новость;
2. срочная новость;
3. новость с возможной бизнес/продуктовой гипотезой..
```

Проверить:

```text
- JSON парсится;
- scores в диапазоне 0..100;
- summary не пустой;
- RawResponseJson сохраняется;
- bad JSON даёт понятную ошибку;
- Provider=Mock работает без ключей;
- AI_PROVIDER=Yandex реально переключает провайдера.
```

## Ограничения MVP

```text
- нет circuit breaker;
- нет embeddings;
- нет RAG;
- нет MCP Gateway;
- нет function calling;
- нет отдельного хранилища prompt versions.
```
