using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AllocationHub.Core.Ai;

namespace AllocationHub.Infrastructure.Ai;

/// <summary>
/// One chat client for every OpenAI-compatible provider — Groq, OpenRouter, OpenAI and local Ollama all
/// expose POST {baseUrl}/chat/completions with the same schema. Switching providers is data, not code:
/// only base URL, model and key differ. Guardrails: bounded max_tokens, low temperature, hard timeout.
/// </summary>
public class OpenAiCompatibleChatClient : IChatClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string? _apiKey;
    private readonly int _maxTokens;

    public OpenAiCompatibleChatClient(HttpClient http, LlmProviderOptions options, string? apiKey, int maxTokens)
    {
        _http = http;
        Provider = options.Name;
        Model = options.Model;
        _baseUrl = options.BaseUrl.TrimEnd('/');
        _apiKey = apiKey;
        _maxTokens = maxTokens;
    }

    public string Provider { get; }
    public string Model { get; }

    public async Task<string> CompleteAsync(string system, string user, CancellationToken ct = default)
    {
        var payload = new ChatRequest(
            Model,
            new[] { new ChatMessage("system", system), new ChatMessage("user", user) },
            Temperature: 0.2,
            MaxTokens: _maxTokens);

        using var req = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload, JsonOpts), Encoding.UTF8, "application/json")
        };
        if (!string.IsNullOrWhiteSpace(_apiKey))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        using var res = await _http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();

        var json = await res.Content.ReadAsStringAsync(ct);
        var parsed = JsonSerializer.Deserialize<ChatResponse>(json, JsonOpts);
        return parsed?.Choices?.FirstOrDefault()?.Message?.Content?.Trim() ?? string.Empty;
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private record ChatRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IReadOnlyList<ChatMessage> Messages,
        [property: JsonPropertyName("temperature")] double Temperature,
        [property: JsonPropertyName("max_tokens")] int MaxTokens);

    private record ChatMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private record ChatResponse([property: JsonPropertyName("choices")] List<Choice>? Choices);
    private record Choice([property: JsonPropertyName("message")] ChatMessage? Message);
}
