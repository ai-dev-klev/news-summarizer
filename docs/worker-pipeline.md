# Worker pipeline

Worker запускает MVP-pipeline без ручных debug endpoint вызовов.

## Что запускается

Порядок job:

```text
1. FetchNews
2. AnalyzeArticles
3. BuildDailyDigests
4. BuildOpportunityDigests
5. SendUrgentNotifications
6. SendPendingNotifications
7. CleanupExpiredData
```

Каждая job выполняется в отдельном DI scope. Ошибка одной job логируется и не останавливает весь pipeline, если `StopOnFatalError=false`.

## Конфигурация

Файл:

```text
src/NewsSummarizer.Worker/appsettings.json
```

Главная секция:

```json
{
  "WorkerPipeline": {
    "Enabled": true,
    "RunOnStartup": true,
    "LoopDelaySeconds": 30,
    "StopOnFatalError": false,
    "AnalyzeArticlesLimit": 20,
    "SendPendingNotificationsLimit": 50,
    "Jobs": {
      "FetchNews": {
        "Enabled": true,
        "RunOnStartup": true,
        "IntervalSeconds": 300
      }
    }
  }
}
```

## Telegram

В Worker Telegram polling отключен:

```json
{
  "Telegram": {
    "PollingEnabled": false
  }
}
```

Это нужно, чтобы API мог принимать команды бота, а Worker только отправлял pending notifications.

## Запуск

```powershell
docker compose up -d

dotnet ef database update `
  --project src/NewsSummarizer.Infrastructure `
  --startup-project src/NewsSummarizer.Api

dotnet run --project src/NewsSummarizer.Worker
```

## Настройки через .env

Worker сам ищет `.env` в корне репозитория и родительских директориях.

Минимально:

```text
NEWS_FETCHING_PROVIDER=Mock
AI_PROVIDER=Mock
TELEGRAM_BOT_TOKEN=...
```

Для Yandex:

```text
AI_PROVIDER=Yandex
YANDEX_AI_API_KEY=...
YANDEX_AI_FOLDER_ID=...
YANDEX_AI_MODEL=yandexgpt/latest
```

## Проверка

После запуска Worker должен логировать:

```text
Worker pipeline started.
Worker job started. Job: FetchNews
Worker job finished. Job: FetchNews
...
```

В Telegram:

```text
/start
/digest
/opportunities
```
