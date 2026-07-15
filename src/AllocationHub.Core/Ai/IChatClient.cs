namespace AllocationHub.Core.Ai;

/// <summary>
/// A minimal chat-completion port. Every provider we support (Groq, OpenRouter, OpenAI, local Ollama)
/// speaks the same OpenAI-compatible protocol, so a single implementation serves all of them — only the
/// base URL, model and API key change. Declared in Core so nothing in the domain depends on a vendor.
/// </summary>
public interface IChatClient
{
    /// <summary>Provider identifier this client talks to (e.g. "groq").</summary>
    string Provider { get; }

    /// <summary>Model this client requests (e.g. "llama-3.1-8b-instant").</summary>
    string Model { get; }

    Task<string> CompleteAsync(string system, string user, CancellationToken ct = default);
}
