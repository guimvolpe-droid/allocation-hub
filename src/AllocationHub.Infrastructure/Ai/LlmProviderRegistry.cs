using AllocationHub.Core.Ai;

namespace AllocationHub.Infrastructure.Ai;

/// <summary>
/// Builds one chat client per USABLE provider — enabled in config AND (keyless OR its API key present in
/// the environment). A configured-but-keyless Groq simply isn't offered, so the app degrades to the
/// deterministic explanation instead of failing. This is the object that makes "multi-LLM" real: it holds
/// several providers at once and resolves them by name.
/// </summary>
public class LlmProviderRegistry : ILlmProviderRegistry
{
    private readonly Dictionary<string, IChatClient> _clients = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _order = new();

    public LlmProviderRegistry(LlmOptions options, Func<HttpClient> httpFactory, Func<string, string?> keyLookup)
    {
        foreach (var p in options.Providers)
        {
            if (!p.Enabled || string.IsNullOrWhiteSpace(p.BaseUrl) || string.IsNullOrWhiteSpace(p.Model))
                continue;

            var key = string.IsNullOrWhiteSpace(p.ApiKeyEnv) ? null : keyLookup(p.ApiKeyEnv!);
            if (!string.IsNullOrWhiteSpace(p.ApiKeyEnv) && string.IsNullOrWhiteSpace(key))
                continue; // needs a key but none is set — not usable

            _clients[p.Name] = new OpenAiCompatibleChatClient(httpFactory(), p, key, options.MaxTokens);
            _order.Add(p.Name);
        }

        DefaultProvider = _order.Contains(options.DefaultProvider, StringComparer.OrdinalIgnoreCase)
            ? options.DefaultProvider
            : _order.FirstOrDefault() ?? string.Empty;
    }

    public IReadOnlyList<string> Providers => _order;
    public string DefaultProvider { get; }

    public IChatClient? Resolve(string? provider = null)
    {
        var name = string.IsNullOrWhiteSpace(provider) ? DefaultProvider : provider!;
        return _clients.TryGetValue(name, out var client) ? client : null;
    }
}
