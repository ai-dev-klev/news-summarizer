using NewsSummarizer.Ai;
using NewsSummarizer.Core;
using NewsSummarizer.Infrastructure;
using NewsSummarizer.Telegram;
using NewsSummarizer.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddCore();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAi(builder.Configuration);
builder.Services.AddTelegram(builder.Configuration);
builder.Services.AddHostedService<WorkerService>();

var host = builder.Build();
host.Run();