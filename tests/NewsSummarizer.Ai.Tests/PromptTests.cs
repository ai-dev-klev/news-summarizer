using NewsSummarizer.Ai.Prompts;
using NewsSummarizer.Core.Entities;

namespace NewsSummarizer.Ai.Tests;

public sealed class PromptTests
{
    [Fact]
    public void NewsClassificationPrompt_ShouldIncludeArticleFieldsAndJsonInstruction()
    {
        var article = new NewsArticle
        {
            Id = Guid.NewGuid(),
            SourceId = Guid.NewGuid(),
            Title = "AI startup market grows",
            Url = "https://example.com/news",
            Description = "Description",
            Content = "Content",
            Language = "en",
            PublishedAt = new DateTimeOffset(2026, 5, 17, 12, 0, 0, TimeSpan.Zero)
        };

        var prompt = NewsClassificationPrompt.Build(article);

        Assert.Contains("Analyze this news article", prompt);
        Assert.Contains("Title:", prompt);
        Assert.Contains(article.Title, prompt);
        Assert.Contains("Url:", prompt);
        Assert.Contains(article.Url, prompt);
        Assert.Contains("Content:", prompt);
        Assert.Contains("JSON", prompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NewsClassificationPrompt_ShouldTrimLongContent()
    {
        var article = new NewsArticle
        {
            Id = Guid.NewGuid(),
            SourceId = Guid.NewGuid(),
            Title = "Title",
            Url = "https://example.com/news",
            Content = new string('x', 9000)
        };

        var prompt = NewsClassificationPrompt.Build(article);

        Assert.Contains("[content truncated]", prompt);
        Assert.True(prompt.Length < 8900);
    }

    [Fact]
    public void DetailedAnalysisPrompt_ShouldIncludePreferencesAndArticleFields()
    {
        var article = new NewsArticle
        {
            Id = Guid.NewGuid(),
            SourceId = Guid.NewGuid(),
            Title = "Research article",
            Url = "https://example.com/research",
            Description = "Description",
            Content = "Content",
            Language = "en"
        };

        var preferences = new UserPreferences
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            EnabledCategories = ["research", "technology"],
            UrgentTopics = ["market_crash"],
            ImportantTopicsText = "AI research",
            ExcludedTopicsText = "celebrity gossip"
        };

        var prompt = DetailedAnalysisPrompt.Build(article, preferences);

        Assert.Contains("Required sections", prompt);
        Assert.Contains("EnabledCategories", prompt);
        Assert.Contains("research", prompt);
        Assert.Contains("UrgentTopics", prompt);
        Assert.Contains("market_crash", prompt);
        Assert.Contains(article.Title, prompt);
        Assert.Contains(article.Url, prompt);
    }
}