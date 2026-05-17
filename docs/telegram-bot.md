# Telegram bot

Telegram bot is enabled when `TELEGRAM_BOT_TOKEN` is configured.

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

## Settings

The main settings command is:

```text
/settings
```

It opens inline buttons for:

```text
categories
urgent topics
max digest size
daily digest on/off
opportunity digest on/off
urgent notifications on/off
```

Categories and urgent topics are selected by pressing buttons. Text commands are still supported:

```text
/categories technology business science
/categories технологии бизнес наука
/urgent_topics crisis security market
/urgent_topics кризис безопасность рынок
/max_items 5
```

## Buttons

Reply keyboard:

```text
/digest
/opportunities
/settings
/help
```

Inline keyboard under `/settings`:

```text
Категории
Срочные темы
Размер сводки
Ежедневная: вкл/выкл
Возможности: вкл/выкл
Срочные: вкл/выкл
```

## Local check

```powershell
dotnet run --project src/NewsSummarizer.Api
```

Then in Telegram:

```text
/start
/settings
```
