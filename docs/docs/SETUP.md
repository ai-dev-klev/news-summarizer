# Запуск и конфигурация

## Требования

- .NET SDK 9
- Docker и Docker Compose
- PostgreSQL через `docker-compose.yml`
- Telegram Bot Token
- Yandex Cloud Folder ID и API key для режима `AI_PROVIDER=Yandex`

## Структура конфигурации

Проект читает настройки из:

1. переменных окружения;
2. файла `.env` в корне репозитория;
3. `appsettings.json`.

Переменные окружения имеют приоритет над `.env` и `appsettings.json`.

## Важное правило безопасности

Файл `.env` не должен попадать в Git. В репозитории должен быть только `.env.example` с пустыми значениями секретов.

Если реальные ключи уже попадали в GitHub, их нужно перевыпустить в Telegram BotFather и Yandex Cloud.

## Минимальный `.env` для локального mock-режима

```env
POSTGRES_DB=news_summarizer
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
POSTGRES_PORT=5432

NEWS_FETCHING_PROVIDER=Mock
AI_PROVIDER=Mock

TELEGRAM_BOT_TOKEN=<telegram_bot_token>
```

Этот режим удобен для стабильного демо без внешних RSS и Yandex AI.

## `.env` для реального режима с Yandex AI Studio

```env
POSTGRES_DB=news_summarizer
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
POSTGRES_PORT=5432

NEWS_FETCHING_PROVIDER=Rss
AI_PROVIDER=Yandex

TELEGRAM_BOT_TOKEN=<telegram_bot_token>

YANDEX_AI_API_KEY=<yandex_api_key>
YANDEX_AI_FOLDER_ID=<yandex_folder_id>
YANDEX_AI_BASE_URL=https://ai.api.cloud.yandex.net/v1
YANDEX_AI_MODEL=yandexgpt/latest
YANDEX_AI_PROMPT_VERSION=v1
```

## Опциональные embeddings

```env
EMBEDDINGS_ENABLED=true
EMBEDDINGS_PROVIDER=Yandex
YANDEX_EMBEDDINGS_MODEL=text-search-doc/latest
YANDEX_EMBEDDINGS_DIMENSIONS=

SEMANTIC_DEDUPLICATION_ENABLED=true
SEMANTIC_DEDUPLICATION_LOOKBACK_HOURS=48
SEMANTIC_DEDUPLICATION_RECENT_CANDIDATE_LIMIT=500
SEMANTIC_DEDUPLICATION_DUPLICATE_THRESHOLD=0.92
SEMANTIC_DEDUPLICATION_MIN_TEXT_LENGTH=20
```

Embeddings нужны для поиска смысловых дублей: разные статьи могут иметь разные заголовки и URL, но описывать одно событие.

## Установка dotnet-ef

```bash
dotnet tool install --global dotnet-ef
```

Если инструмент уже установлен:

```bash
dotnet tool update --global dotnet-ef
```

## Запуск PostgreSQL

```bash
docker compose up -d
```

Проверка контейнера:

```bash
docker ps
```

## Восстановление зависимостей и сборка

```bash
dotnet restore
dotnet build
```

## Применение миграций

```bash
dotnet ef database update \
  --project src/NewsSummarizer.Infrastructure \
  --startup-project src/NewsSummarizer.Api
```

PowerShell-вариант:

```powershell
dotnet ef database update `
  --project src/NewsSummarizer.Infrastructure `
  --startup-project src/NewsSummarizer.Api
```

## Запуск API

```bash
dotnet run --project src/NewsSummarizer.Api
```

API нужен для:

- health check;
- debug endpoints;
- Telegram polling;
- команд пользователя в Telegram.

Health check:

```bash
curl http://localhost:5000/health
```

## Запуск Worker

В отдельном терминале:

```bash
dotnet run --project src/NewsSummarizer.Worker
```

Worker выполняет pipeline:

```text
FetchNews
AnalyzeArticles
BuildDailyDigests
BuildOpportunityDigests
SendUrgentNotifications
SendPendingNotifications
CleanupExpiredData
```

## Проверка MVP через Debug API

```bash
curl -X POST http://localhost:5000/debug/seed
curl -X POST http://localhost:5000/debug/fetch-news
curl -X POST "http://localhost:5000/debug/analyze-articles?limit=20"
curl -X POST http://localhost:5000/debug/build-daily-digests
curl -X POST http://localhost:5000/debug/build-opportunity-digests
curl -X POST http://localhost:5000/debug/send-urgent-notifications
curl -X POST "http://localhost:5000/debug/send-pending-notifications?limit=50"
curl http://localhost:5000/debug/articles/recent
curl http://localhost:5000/debug/digests/recent
curl http://localhost:5000/debug/notifications/recent
```

## Проверка Telegram

В Telegram открыть бота и выполнить:

```text
/start
/settings
/digest
/opportunities
/analyze <articleId>
```

`articleId` можно взять из `GET /debug/articles/recent`.

## Рекомендуемые режимы для демо

### Стабильное демо

```env
NEWS_FETCHING_PROVIDER=Mock
AI_PROVIDER=Mock
```

Плюсы: не зависит от внешних сервисов.

### Реальное AI-демо

```env
NEWS_FETCHING_PROVIDER=Rss
AI_PROVIDER=Yandex
```

Плюсы: показывает реальную интеграцию с Yandex AI Studio.

### Реальное AI-демо с семантической дедупликацией

```env
NEWS_FETCHING_PROVIDER=Rss
AI_PROVIDER=Yandex
EMBEDDINGS_ENABLED=true
SEMANTIC_DEDUPLICATION_ENABLED=true
```

Плюсы: демонстрирует дополнительное использование embeddings.
