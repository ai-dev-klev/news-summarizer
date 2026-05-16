# Debug API

The debug API is used for local MVP pipeline checks.

## Run database

```powershell
docker compose up -d
```

## Apply migrations

```powershell
dotnet ef database update --project src/NewsSummarizer.Infrastructure --startup-project src/NewsSummarizer.Api
```

## Run API

```powershell
dotnet run --project src/NewsSummarizer.Api
```

## Seed data

```powershell
Invoke-RestMethod -Method Post http://localhost:5000/debug/seed
```

## Fetch mock news

```powershell
Invoke-RestMethod -Method Post http://localhost:5000/debug/fetch-news
```

## Analyze pending articles

```powershell
Invoke-RestMethod -Method Post "http://localhost:5000/debug/analyze-articles?limit=20"
```

## Build daily digests

```powershell
Invoke-RestMethod -Method Post http://localhost:5000/debug/build-daily-digests
```

## Build opportunity digests

```powershell
Invoke-RestMethod -Method Post http://localhost:5000/debug/build-opportunity-digests
```

## Create urgent notifications

```powershell
Invoke-RestMethod -Method Post http://localhost:5000/debug/send-urgent-notifications
```

## Cleanup expired data

```powershell
Invoke-RestMethod -Method Post http://localhost:5000/debug/cleanup
```

## See recent articles

```powershell
Invoke-RestMethod http://localhost:5000/debug/articles/recent | ConvertTo-Json -Depth 10
```

## See recent digests

```powershell
Invoke-RestMethod http://localhost:5000/debug/digests/recent | ConvertTo-Json -Depth 10
```

## See recent notifications

```powershell
Invoke-RestMethod http://localhost:5000/debug/notifications/recent | ConvertTo-Json -Depth 10
```

## See sources

```powershell
Invoke-RestMethod http://localhost:5000/debug/sources
```

## See users

```powershell
Invoke-RestMethod http://localhost:5000/debug/users
```

## Expected mock flow

```text
POST /debug/seed
POST /debug/fetch-news
POST /debug/analyze-articles
POST /debug/build-daily-digests
POST /debug/build-opportunity-digests
GET  /debug/digests/recent
```