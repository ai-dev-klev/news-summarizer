
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using NewsSummarizer.Ai.Models;
using NewsSummarizer.Core.Enums;
using NewsSummarizer.Core.Interfaces;
using NewsSummarizer.Core.Models;

namespace NewsSummarizer.Ai.Embeddings;

public sealed class YandexEmbeddingProvider : IEmbeddingProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;
    private readonly EmbeddingProviderOptions _options;

    public YandexEmbeddingProvider(
        HttpClient httpClient,
        IOptions<EmbeddingProviderOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public bool IsEnabled =>
        _options.Enabled &&
        _options.Provider.Equals("Yandex", StringComparison.OrdinalIgnoreCase) &&
        !string.IsNullOrWhiteSpace(_options.ApiKey) &&
        !string.IsNullOrWhiteSpace(_options.FolderId) &&
        !string.IsNullOrWhiteSpace(_options.Model);

    public AiProviderType Provider => AiProviderType.Yandex;

    public string Model => BuildModelUri();

    public async Task<EmbeddingResult> CreateEmbeddingAsync(
        string input,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
        {
            throw new InvalidOperationException("Yandex embeddings are disabled or not fully configured.");
        }

        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("Embedding input must not be empty.", nameof(input));
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _options.RequestTimeoutSeconds)));

        var request = new YandexEmbeddingRequest(
            Input: input,
            Model: BuildModelUri(),
            EncodingFormat: "float",
            Dimensions: _options.Dimensions > 0 ? _options.Dimensions : null);

        using var response = await _httpClient.PostAsJsonAsync(
            "embeddings",
            request,
            JsonOptions,
            timeoutCts.Token);

        var body = await response.Content.ReadAsStringAsync(timeoutCts.Token);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Yandex embeddings request failed. Status: {(int)response.StatusCode}. Body: {body}");
        }

        var parsed = JsonSerializer.Deserialize<YandexEmbeddingResponse>(body, JsonOptions);
        var embedding = parsed?.Data?.FirstOrDefault()?.Embedding;

        if (embedding is null || embedding.Length == 0)
        {
            throw new InvalidOperationException("Yandex embeddings response does not contain an embedding vector.");
        }

        return new EmbeddingResult(Provider, parsed?.Model ?? BuildModelUri(), embedding);
    }

    private string BuildModelUri()
    {
        var model = _options.Model.Trim();

        if (model.StartsWith("emb://", StringComparison.OrdinalIgnoreCase))
        {
            return model;
        }

        return $"emb://{_options.FolderId}/{model}";
    }

    private sealed record YandexEmbeddingRequest(
        string Input,
        string Model,
        [property: JsonPropertyName("encoding_format")]
        string EncodingFormat,
        int? Dimensions);

    private sealed record YandexEmbeddingResponse(
        IReadOnlyList<YandexEmbeddingData> Data,
        string Model,
        string Object,
        YandexEmbeddingUsage Usage);

    private sealed record YandexEmbeddingData(
        int Index,
        float[] Embedding,
        string Object);

    private sealed record YandexEmbeddingUsage(
        [property: JsonPropertyName("prompt_tokens")]
        int PromptTokens,
        [property: JsonPropertyName("total_tokens")]
        int TotalTokens);
}
