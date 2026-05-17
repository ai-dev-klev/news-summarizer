# Telegram bot

Telegram bot is enabled when `TELEGRAM_BOT_TOKEN` is configured.

## Configuration

`.env` / environment variable:

```text
TELEGRAM_BOT_TOKEN=...
```

`appsettings.Development.json` alternative:

```json
{
  "Telegram": {
    "BotToken": "...",
    "PollingEnabled": true,
    "PollingTimeoutSeconds": 25,
    "SendChunkLength": 3800
  }
}
```

## Commands

```text
/start
/help
/status
/digest
/opportunities
/settings
/analyze <articleId>
```

## User settings commands

```text
/settings
/categories technology business science
/categories технологии бизнес наука
/urgent_topics crisis security market
/urgent_topics кризис безопасность рынок
/max_items 5
/daily_on
/daily_off
/opportunities_on
/opportunities_off
/urgent_on
/urgent_off
```

Supported category aliases include:

```text
general, world, business, technology, science, politics, security, education, health, culture, sports, startups
общие, мир, бизнес, технологии, наука, политика, безопасность, образование, здоровье, культура, спорт, стартапы
```

## Buttons

The bot sends a persistent reply keyboard with these buttons:

```text
/digest
/opportunities
/settings
/help
```

The bot also registers Telegram command menu through `setMyCommands`.

If buttons are not visible:

```text
1. Stop the API process.
2. Start it again after applying this patch.
3. Send /start to the bot.
4. In Telegram, check the small keyboard/menu icon near the message input.
```

## Local MVP check

```powershell
docker compose up -d

dotnet ef database update `
  --project src/NewsSummarizer.Infrastructure `
  --startup-project src/NewsSummarizer.Api

dotnet run --project src/NewsSummarizer.Api
```

In another terminal:

```powershell
Invoke-RestMethod -Method Post http://localhost:5000/debug/seed
Invoke-RestMethod -Method Post http://localhost:5000/debug/fetch-news
Invoke-RestMethod -Method Post "http://localhost:5000/debug/analyze-articles?limit=20"
Invoke-RestMethod -Method Post http://localhost:5000/debug/build-daily-digests
Invoke-RestMethod -Method Post http://localhost:5000/debug/build-opportunity-digests
Invoke-RestMethod -Method Post http://localhost:5000/debug/send-urgent-notifications
Invoke-RestMethod -Method Post http://localhost:5000/debug/send-pending-notifications
```

Then use Telegram:

```text
/start
/settings
/digest
/opportunities
/analyze <articleId>
```

## Notes

- The bot uses long polling.
- If `TELEGRAM_BOT_TOKEN` is empty, polling is disabled and the application still starts.
- Pending notifications are sent by `SendPendingNotificationsUseCase`.
