# Local .env loading

The API loads `.env` on startup before `WebApplication.CreateBuilder(args)`.

Search order:

```text
current working directory
src/NewsSummarizer.Api
parents of current working directory
parents of AppContext.BaseDirectory
```

Process environment variables have priority over `.env`.

Required Telegram value:

```text
TELEGRAM_BOT_TOKEN=...
```

If Telegram text looks broken, make sure source files with Russian text are saved as UTF-8.