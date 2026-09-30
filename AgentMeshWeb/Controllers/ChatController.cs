using AgentMeshWeb.Models;
using AgentMeshWeb.Services;
using Microsoft.AspNetCore.Mvc;

namespace AgentMeshWeb.Controllers;

public sealed class ChatController(IAgentMeshApiClient apiClient) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            return View(new ChatPageViewModel
            {
                Configuration = await apiClient.GetConfigurationSummaryAsync(cancellationToken)
            });
        }
        catch (Exception)
        {
            return View(new ChatPageViewModel
            {
                ConfigurationError = "Configuration details are currently unavailable."
            });
        }
    }
}