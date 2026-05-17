using NewsSummarizer.Ai;
using NewsSummarizer.Core;
using NewsSummarizer.Infrastructure;
using NewsSummarizer.Telegram;
using NewsSummarizer.Worker;

LocalEnvLoader.Load();

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddCore();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAi(builder.Configuration);
builder.Services.AddTelegram(builder.Configuration);
builder.Services.AddWorkerPipeline(builder.Configuration);

var host = builder.Build();
host.Run();
