# Архитектура

## Назначение системы

`news-summarizer` — сервис, который автоматически собирает новости, анализирует их через AI и отправляет пользователю персональные сводки в Telegram.

Главная идея архитектуры: AI встроен в pipeline обработки данных и влияет на итоговое решение системы, а не просто отвечает на свободный вопрос пользователя.

## Общая схема

```text
News Sources
  -> Infrastructure Fetchers
  -> Core Use Cases
  -> PostgreSQL
  -> Yandex AI / Yandex Embeddings
  -> Digest / Notification Builder
  -> Telegram Bot
```

## Проекты решения

```text
src/NewsSummarizer.Core
src/NewsSummarizer.Infrastructure
src/NewsSummarizer.Ai
src/NewsSummarizer.Telegram
src/NewsSummarizer.Api
src/NewsSummarizer.Worker
tests/*
```

### `NewsSummarizer.Core`

Содержит бизнес-логику, сущности, интерфейсы и use cases.

Основные use cases:

- `FetchNewsUseCase` — получить новости из источников;
- `AnalyzeArticleUseCase` — проанализировать новости через AI;
- `BuildDailyDigestUseCase` — собрать ежедневный дайджест;
- `BuildOpportunityDigestUseCase` — собрать сводку возможностей;
- `SendUrgentNotificationsUseCase` — создать срочные уведомления;
- `SendPendingNotificationsUseCase` — отправить pending-уведомления;
- `AnalyzeArticleInDetailUseCase` — подготовить подробный AI-анализ новости;
- `CleanupExpiredDataUseCase` — удалить устаревшие данные.

### `NewsSummarizer.Infrastructure`

Содержит техническую реализацию хранения и получения данных:

- EF Core `DbContext`;
- репозитории;
- PostgreSQL migrations;
- RSS fetcher;
- mock fetcher;
- seed данных для локального запуска.

### `NewsSummarizer.Ai`

Содержит интеграцию с Yandex AI Studio:

- `YandexAiProvider` — анализ новости и подробный анализ;
- `YandexChatClientFactory` — создание клиента для OpenAI-compatible API;
- `AiResponseParser` — разбор JSON-ответа модели;
- prompts для классификации новости и подробного анализа;
- `YandexEmbeddingProvider` — получение embeddings;
- семантическая дедупликация через cosine similarity.

### `NewsSummarizer.Telegram`

Содержит Telegram-интерфейс:

- polling;
- обработка команд;
- inline settings;
- отправка сообщений;
- форматирование дайджестов.

### `NewsSummarizer.Api`

Minimal API для локальной проверки и запуска Telegram-интеграции.

Основные endpoints:

- `GET /health`
- `POST /debug/seed`
- `POST /debug/fetch-news`
- `POST /debug/analyze-articles`
- `POST /debug/build-daily-digests`
- `POST /debug/build-opportunity-digests`
- `POST /debug/send-urgent-notifications`
- `POST /debug/send-pending-notifications`
- `GET /debug/articles/recent`
- `GET /debug/digests/recent`
- `GET /debug/notifications/recent`

### `NewsSummarizer.Worker`

Фоновый процесс, который автоматически запускает pipeline по расписанию.

Порядок задач:

```text
1. FetchNews
2. AnalyzeArticles
3. BuildDailyDigests
4. BuildOpportunityDigests
5. SendUrgentNotifications
6. SendPendingNotifications
7. CleanupExpiredData
```

## Основной пользовательский сценарий

```text
Пользователь открывает Telegram-бота
  -> /start создаёт профиль и настройки
  -> система собирает новости
  -> AI анализирует новости
  -> система формирует дайджест под настройки пользователя
  -> пользователь получает /digest или /opportunities
  -> при необходимости пользователь вызывает /analyze <articleId>
```

## Pipeline обработки новости

### 1. Сбор

Источник новости описывается в `news_sources`. Fetcher получает список материалов из mock-источника или RSS.

### 2. Нормализация

Для каждой новости вычисляются:

- normalized title;
- canonical URL;
- content hash;
- dedup key;
- срок хранения.

### 3. Базовая дедупликация

Система отбрасывает очевидные повторы по URL, canonical URL, content hash, title и dedup key.

### 4. AI-анализ

Yandex AI Studio получает заголовок, описание и контент статьи. Модель возвращает строгий JSON:

```json
{
  "category": "technology",
  "importanceScore": 80,
  "urgencyScore": 20,
  "opportunityScore": 70,
  "summary": "Short summary",
  "reason": "Why it matters",
  "opportunityReason": "Why it may be useful",
  "dailyDigestCandidate": true,
  "opportunityDigestCandidate": true,
  "urgentCandidate": false
}
```

Эти поля сохраняются в `article_ai_results` и используются дальше при выборе новостей.

### 5. Семантическая дедупликация

Если включены embeddings, система строит embedding-вектор по тексту новости и сравнивает его с недавними новостями.

Если cosine similarity выше порога, новость помечается как дубликат.

### 6. Формирование дайджеста

Система берёт успешные AI-результаты и выбирает новости с учётом:

- категорий пользователя;
- важности;
- срочности;
- opportunity score;
- максимального размера дайджеста.

### 7. Уведомление

Сформированный digest или urgent notification сохраняется в базе и отправляется пользователю через Telegram.

## Основные таблицы

- `users` — Telegram-пользователи.
- `user_preferences` — категории, urgent topics, настройки дайджестов.
- `news_sources` — источники новостей.
- `news_articles` — загруженные статьи и статус обработки.
- `article_ai_results` — структурированный AI-анализ статьи.
- `article_embeddings` — embedding-векторы для семантической дедупликации.
- `digests` — созданные дайджесты.
- `digest_items` — элементы дайджеста.
- `notifications` — pending/sent/failed уведомления.
- `detailed_analyses` — результаты подробного AI-анализа.

## Почему AI является важной частью продукта

Без AI сервис был бы обычным RSS-агрегатором. AI добавляет продуктовую ценность:

- превращает сырую новость в структурированные признаки;
- оценивает важность и срочность;
- объясняет, почему новость полезна;
- помогает находить возможности и гипотезы;
- уменьшает повторы через embeddings;
- даёт подробный анализ по запросу пользователя.

## Ограничения архитектуры MVP

- нет отдельной очереди сообщений;
- нет полноценного retry/circuit breaker слоя для внешних API;
- debug endpoints не предназначены для production;
- настройки пользователя пока простые;
- Telegram — основной интерфейс, web UI не реализован.

## Возможное развитие

- добавить web UI;
- вынести фоновые задачи в очередь;
- добавить RAG по архиву новостей;
- расширить персонализацию;
- добавить function calling/tools;
- добавить голосовой сценарий через Алису;
- добавить мониторинг, метрики и admin panel.
