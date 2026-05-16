using NewsSummarizer.Ai;
using NewsSummarizer.Core;
using NewsSummarizer.Infrastructure;
using NewsSummarizer.Telegram;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCore();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAi(builder.Configuration);
builder.Services.AddTelegram(builder.Configuration);

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "news-summarizer" }));
app.MapPost("/debug/fetch-news", () => Results.Accepted());
app.MapPost("/debug/analyze-articles", () => Results.Accepted());
app.MapPost("/debug/build-daily-digests", () => Results.Accepted());
app.MapPost("/debug/build-opportunity-digests", () => Results.Accepted());
app.MapPost("/debug/send-test-urgent", () => Results.Accepted());

app.Run();