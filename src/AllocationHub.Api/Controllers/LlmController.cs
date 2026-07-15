using AllocationHub.Core.Ai;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AllocationHub.Api.Controllers;

/// <summary>Exposes which LLM providers are usable right now, so the UI can offer a live provider switch.</summary>
[ApiController]
[Authorize]
[Route("api/llm")]
public class LlmController : ControllerBase
{
    private readonly ILlmProviderRegistry _registry;

    public LlmController(ILlmProviderRegistry registry) => _registry = registry;

    public record LlmProvidersDto(bool Available, string DefaultProvider, IReadOnlyList<string> Providers);

    [HttpGet("providers")]
    public ActionResult<LlmProvidersDto> Providers() =>
        new LlmProvidersDto(_registry.AnyAvailable, _registry.DefaultProvider, _registry.Providers);
}
