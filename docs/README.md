# news-summarizer

`news-summarizer` — Telegram-сервис персональных новостных сводок на базе Yandex AI Studio и Алиса AI.

Сервис собирает новости из RSS-источников, удаляет дубликаты, анализирует материалы через AI, выбирает важные новости под интересы пользователя и отправляет краткие сводки в Telegram.

## Проблема

Пользователь получает слишком много новостей из разных источников. Большая часть потока повторяется, плохо структурирована и не отвечает на вопрос: «что действительно важно именно для меня?»

## Решение

Сервис превращает поток новостей в персональную AI-сводку:

```text
RSS / mock sources
  -> нормализация и дедупликация
  -> AI-анализ новости
  -> персональный отбор
  -> дайджест / срочное уведомление
  -> Telegram
```

## Целевой пользователь

- специалист, которому нужно быстро следить за рынком, технологиями, наукой или бизнесом;
- студент или исследователь, который хочет получать короткую выжимку по выбранным темам;
- команда, которой нужен общий канал важных новостей и возможностей.

## Что реализовано

- Telegram-бот с командами `/start`, `/help`, `/status`, `/digest`, `/opportunities`, `/settings`, `/analyze <articleId>`.
- Хранение пользователей, настроек, источников, новостей, AI-результатов, дайджестов и уведомлений в PostgreSQL.
- Сбор новостей из mock-источников и RSS-источников.
- Базовая дедупликация по URL, canonical URL, заголовку и content hash.
- AI-анализ новости через Yandex AI Studio:
  - категория;
  - важность;
  - срочность;
  - opportunity score;
  - краткое summary;
  - причина попадания в дайджест.
- Подробный AI-анализ отдельной новости по команде `/analyze`.
- Опциональные Yandex Embeddings для семантической дедупликации похожих новостей.
- Worker pipeline для автоматического выполнения фоновых задач.
- Debug API для локальной демонстрации MVP-сценария.

## Как используется Yandex AI Studio / Алиса AI

AI в проекте не является обычным чатом. Он встроен в продуктовый pipeline:

1. **Структурирует новость** — возвращает JSON с категорией, важностью, срочностью и summary.
2. **Помогает принимать решение** — определяет, попадёт ли новость в ежедневную сводку, сводку возможностей или срочное уведомление.
3. **Генерирует объяснение** — пишет, почему новость важна для пользователя.
4. **Делает подробный анализ** — по запросу пользователя объясняет событие, последствия, риски и возможные гипотезы.
5. **Семантически сравнивает новости** — embeddings помогают находить новости об одном событии, написанные разными словами.

## Технологии

- Backend: .NET 9
- Database: PostgreSQL
- ORM: Entity Framework Core + Npgsql
- AI: Yandex AI Studio, OpenAI-compatible API
- Embeddings: Yandex Embeddings
- Bot: Telegram Bot API
- Background jobs: .NET Worker Service
- Infrastructure: Docker Compose
- Tests: xUnit

## Быстрый запуск

### 1. Подготовить окружение

```bash
cp .env.example .env
```

Заполнить `.env` своими ключами. Не коммитить `.env` в Git.

Минимально для mock-режима:

```env
NEWS_FETCHING_PROVIDER=Mock
AI_PROVIDER=Mock
TELEGRAM_BOT_TOKEN=<telegram_bot_token>
```

Для реального AI:

```env
NEWS_FETCHING_PROVIDER=Rss
AI_PROVIDER=Yandex
YANDEX_AI_API_KEY=<yandex_api_key>
YANDEX_AI_FOLDER_ID=<yandex_folder_id>
YANDEX_AI_BASE_URL=https://ai.api.cloud.yandex.net/v1
YANDEX_AI_MODEL=yandexgpt/latest
YANDEX_AI_PROMPT_VERSION=v1
```

### 2. Запустить PostgreSQL

```bash
docker compose up -d
```

### 3. Применить миграции

```bash
dotnet ef database update \
  --project src/NewsSummarizer.Infrastructure \
  --startup-project src/NewsSummarizer.Api
```

### 4. Запустить API

```bash
dotnet run --project src/NewsSummarizer.Api
```

### 5. Запустить Worker

В отдельном терминале:

```bash
dotnet run --project src/NewsSummarizer.Worker
```

## Локальная проверка через Debug API

```bash
curl -X POST http://localhost:5000/debug/seed
curl -X POST http://localhost:5000/debug/fetch-news
curl -X POST "http://localhost:5000/debug/analyze-articles?limit=20"
curl -X POST http://localhost:5000/debug/build-daily-digests
curl -X POST http://localhost:5000/debug/build-opportunity-digests
curl http://localhost:5000/debug/digests/recent
```

Если порт отличается, взять актуальный адрес из логов `dotnet run`.

## Документация

- [Архитектура](docs/ARCHITECTURE.md)
- [Запуск и переменные окружения](docs/SETUP.md)
- [Сценарий демо](docs/DEMO.md)
- [Краткое описание проекта](docs/PROJECT_BRIEF.md)

## Ограничения MVP

- веб-интерфейс не реализован;
- персонализация пока основана на категориях и текстовых предпочтениях;
- качество RSS зависит от доступности внешних источников;
- embeddings и семантическая дедупликация включаются отдельно;
- production-наблюдаемость, retry policy и rate limiting требуют доработки.

## Дальнейшее развитие

- добавить веб-панель для управления источниками и интересами;
- расширить персонализацию через историю действий пользователя;
- подключить больше внешних источников и API;
- добавить RAG по архиву новостей и документам пользователя;
- сделать агентный сценарий через tools/function calling;
- добавить голосовой сценарий через Алису.
