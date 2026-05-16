# EF Core setup

This project uses PostgreSQL through EF Core and Npgsql.

## Apply this script

```powershell
.\apply-efcore-repositories.ps1
```

## Create initial migration

```powershell
dotnet ef migrations add InitialCreate --project src/NewsSummarizer.Infrastructure --startup-project src/NewsSummarizer.Api
```

## Apply migration

```powershell
dotnet ef database update --project src/NewsSummarizer.Infrastructure --startup-project src/NewsSummarizer.Api
```

## Useful checks

```powershell
dotnet build
dotnet ef migrations list --project src/NewsSummarizer.Infrastructure --startup-project src/NewsSummarizer.Api
```