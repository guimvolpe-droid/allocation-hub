namespace AllocationHub.Infrastructure.Ai;

/// <summary>Bound from the "Llm" config section. Keys are NEVER stored here — only the env var name is.</summary>
public class LlmOptions
{
    public string DefaultProvider { get; set; } = string.Empty;
    public List<LlmProviderOptions> Providers { get; set; } = new();

    /// <summary>Output guardrails applied to every LLM explanation.</summary>
    public int MaxTokens { get; set; } = 120;
    public int TimeoutSeconds { get; set; } = 12;
}

public class LlmProviderOptions
{
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;   // e.g. https://api.groq.com/openai/v1
    public string Model { get; set; } = string.Empty;     // e.g. llama-3.1-8b-instant
    public string? ApiKeyEnv { get; set; }                // e.g. GROQ_API_KEY (null for keyless like Ollama)
    public bool Enabled { get; set; }
}
