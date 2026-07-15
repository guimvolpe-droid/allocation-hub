namespace AllocationHub.Core.Ai;

/// <summary>
/// The set of LLM providers that are actually usable right now (enabled in config AND with their API key
/// present in the environment). This is what makes the app "multi-LLM": several providers are configured,
/// resolved by name, and switchable per request — with the matching rule never touching any of them.
/// </summary>
public interface ILlmProviderRegistry
{
    /// <summary>Names of providers currently available (enabled + key present). May be empty.</summary>
    IReadOnlyList<string> Providers { get; }

    /// <summary>The provider used when a request doesn't name one. Empty when nothing is available.</summary>
    string DefaultProvider { get; }

    /// <summary>True when at least one provider is usable.</summary>
    bool AnyAvailable => Providers.Count > 0;

    /// <summary>Resolves a chat client by name, or the default when name is null/blank. Null if none usable.</summary>
    IChatClient? Resolve(string? provider = null);
}
