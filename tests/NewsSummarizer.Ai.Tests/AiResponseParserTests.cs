using NewsSummarizer.Ai.Parsing;

namespace NewsSummarizer.Ai.Tests;

public sealed class AiResponseParserTests
{
    private readonly AiResponseParser _parser = new();

    [Fact]
    public void ParseArticleAnalysis_ShouldParseValidJson()
    {
        var result = _parser.ParseArticleAnalysis(
            """
            {
              "category": "technology",
              "importanceScore": 80,
              "urgencyScore": 20,
              "opportunityScore": 90,
              "summary": "Short summary.",
              "reason": "Important for market.",
              "opportunityReason": "Startup opportunity.",
              "dailyDigestCandidate": true,
              "opportunityDigestCandidate": true,
              "urgentCandidate": false
            }
            """);

        Assert.Equal("technology", result.Category);
        Assert.Equal(80, result.ImportanceScore);
        Assert.Equal(20, result.UrgencyScore);
        Assert.Equal(90, result.OpportunityScore);
        Assert.Equal("Short summary.", result.Summary);
        Assert.Equal("Important for market.", result.Reason);
        Assert.Equal("Startup opportunity.", result.OpportunityReason);
        Assert.True(result.DailyDigestCandidate);
        Assert.True(result.OpportunityDigestCandidate);
        Assert.False(result.UrgentCandidate);
        Assert.Contains("\"category\"", result.RawResponseJson);
    }

    [Fact]
    public void ParseArticleAnalysis_ShouldParseJsonInsideMarkdownFence()
    {
        var result = _parser.ParseArticleAnalysis(
            """
            ```json
            {
              "category": "science",
              "importanceScore": 70,
              "urgencyScore": 10,
              "opportunityScore": 85,
              "summary": "Research summary.",
              "reason": "Research reason.",
              "opportunityReason": "Research opportunity.",
              "dailyDigestCandidate": true,
              "opportunityDigestCandidate": true,
              "urgentCandidate": false
            }
            ```
            """);

        Assert.Equal("science", result.Category);
        Assert.Equal(70, result.ImportanceScore);
        Assert.Equal(85, result.OpportunityScore);
    }

    [Fact]
    public void ParseArticleAnalysis_ShouldExtractJsonFromTextAroundObject()
    {
        var result = _parser.ParseArticleAnalysis(
            """
            Here is the answer:
            {
              "category": "business",
              "importanceScore": 61,
              "urgencyScore": 44,
              "opportunityScore": 33,
              "summary": "Summary with {braces} inside a string.",
              "reason": "Reason.",
              "opportunityReason": "Opportunity.",
              "dailyDigestCandidate": true,
              "opportunityDigestCandidate": false,
              "urgentCandidate": true
            }
            End.
            """);

        Assert.Equal("business", result.Category);
        Assert.Equal("Summary with {braces} inside a string.", result.Summary);
        Assert.True(result.UrgentCandidate);
    }

    [Fact]
    public void ParseArticleAnalysis_ShouldClampScoresToValidRange()
    {
        var result = _parser.ParseArticleAnalysis(
            """
            {
              "category": "general",
              "importanceScore": 150,
              "urgencyScore": -10,
              "opportunityScore": 101,
              "summary": "Summary.",
              "reason": "Reason.",
              "opportunityReason": "Opportunity.",
              "dailyDigestCandidate": true,
              "opportunityDigestCandidate": false,
              "urgentCandidate": true
            }
            """);

        Assert.Equal(100, result.ImportanceScore);
        Assert.Equal(0, result.UrgencyScore);
        Assert.Equal(100, result.OpportunityScore);
    }

    [Fact]
    public void ParseArticleAnalysis_ShouldAcceptStringAndDoubleScores()
    {
        var result = _parser.ParseArticleAnalysis(
            """
            {
              "category": "general",
              "importanceScore": "80",
              "urgencyScore": 19.6,
              "opportunityScore": "42.4",
              "summary": "Summary.",
              "reason": "Reason.",
              "opportunityReason": "Opportunity.",
              "dailyDigestCandidate": "true",
              "opportunityDigestCandidate": "false",
              "urgentCandidate": false
            }
            """);

        Assert.Equal(80, result.ImportanceScore);
        Assert.Equal(20, result.UrgencyScore);
        Assert.Equal(42, result.OpportunityScore);
        Assert.True(result.DailyDigestCandidate);
        Assert.False(result.OpportunityDigestCandidate);
        Assert.False(result.UrgentCandidate);
    }

    [Fact]
    public void ParseArticleAnalysis_ShouldUseFallbacksForMissingOptionalLikeFields()
    {
        var result = _parser.ParseArticleAnalysis(
            """
            {
              "importanceScore": 40,
              "urgencyScore": 12,
              "opportunityScore": 30,
              "summary": "Summary.",
              "reason": "Reason."
            }
            """);

        Assert.Equal("other", result.Category);
        Assert.Equal("Summary.", result.Summary);
        Assert.Equal("Reason.", result.Reason);
        Assert.Equal("Reason.", result.OpportunityReason);
        Assert.False(result.DailyDigestCandidate);
        Assert.False(result.OpportunityDigestCandidate);
        Assert.False(result.UrgentCandidate);
    }

    [Fact]
    public void ParseArticleAnalysis_ShouldUseFallbacksForBlankStringFields()
    {
        var result = _parser.ParseArticleAnalysis(
            """
            {
              "category": "",
              "importanceScore": 40,
              "urgencyScore": 12,
              "opportunityScore": 30,
              "summary": "",
              "reason": "",
              "opportunityReason": "",
              "dailyDigestCandidate": false,
              "opportunityDigestCandidate": false,
              "urgentCandidate": false
            }
            """);

        Assert.Equal("other", result.Category);
        Assert.Equal("No summary provided.", result.Summary);
        Assert.Equal("No reason provided.", result.Reason);
        Assert.Equal("No reason provided.", result.OpportunityReason);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ParseArticleAnalysis_ShouldThrow_WhenResponseIsEmpty(string rawResponse)
    {
        Assert.Throws<InvalidOperationException>(() => _parser.ParseArticleAnalysis(rawResponse));
    }

    [Fact]
    public void ParseArticleAnalysis_ShouldThrow_WhenResponseDoesNotContainJsonObject()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            _parser.ParseArticleAnalysis("plain text"));

        Assert.Contains("does not contain JSON object", exception.Message);
    }

    [Fact]
    public void ParseArticleAnalysis_ShouldThrow_WhenJsonIsMalformed()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            _parser.ParseArticleAnalysis("{ invalid json"));

        Assert.Contains("JSON", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParseArticleAnalysis_ShouldThrow_WhenRequiredScoreIsMissing()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            _parser.ParseArticleAnalysis(
                """
                {
                  "category": "general",
                  "urgencyScore": 20,
                  "opportunityScore": 10,
                  "summary": "Summary.",
                  "reason": "Reason."
                }
                """));

        Assert.Contains("importanceScore", exception.Message);
    }

    [Fact]
    public void ParseArticleAnalysis_ShouldThrow_WhenScoreIsNotNumeric()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            _parser.ParseArticleAnalysis(
                """
                {
                  "category": "general",
                  "importanceScore": "high",
                  "urgencyScore": 20,
                  "opportunityScore": 10,
                  "summary": "Summary.",
                  "reason": "Reason."
                }
                """));

        Assert.Contains("importanceScore", exception.Message);
    }
}