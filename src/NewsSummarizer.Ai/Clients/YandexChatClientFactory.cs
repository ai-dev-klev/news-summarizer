using Microsoft.Extensions.Options;
using NewsSummarizer.Ai.Models;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace NewsSummarizer.Ai.Clients;

public sealed class YandexChatClientFactory
{
    private readonly AiProviderOptions _options;

    public YandexChatClientFactory(IOptions<AiProviderOptions> options)
    {
        _options = options.Value;
    }

    public ChatClient Create()
    {
        ValidateOptions();

        var clientOptions = new OpenAIClientOptions
        {
            Endpoint = new Uri(_options.BaseUrl.TrimEnd('/')),
            ProjectId = _options.FolderId
        };

        var openAiClient = new OpenAIClient(
            new ApiKeyCredential(_options.ApiKey),
            clientOptions);

        return openAiClient.GetChatClient(BuildModelUri());
    }

    public string BuildModelUri()
    {
        ValidateRequired(_options.FolderId, "Yandex AI folder id is not configured.");
        ValidateRequired(_options.Model, "Yandex AI model is not configured.");

        return _options.Model.StartsWith("gpt://", StringComparison.OrdinalIgnoreCase)
            ? _options.Model
            : $"gpt://{_options.FolderId}/{_options.Model}";
    }

    private void ValidateOptions()
    {
        ValidateRequired(_options.ApiKey, "Yandex AI API key is not configured.");
        ValidateRequired(_options.FolderId, "Yandex AI folder id is not configured.");
        ValidateRequired(_options.BaseUrl, "Yandex AI base URL is not configured.");
        ValidateRequired(_options.Model, "Yandex AI model is not configured.");
    }

    private static void ValidateRequired(string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(message);
        }
    }
}
