using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using EnjoyEveryday.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EnjoyEveryday.Infrastructure.Services;

public class HttpRealtimeNotificationService : IRealtimeNotificationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<HttpRealtimeNotificationService> _logger;

    public HttpRealtimeNotificationService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<HttpRealtimeNotificationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task NotifyParentMessageSentAsync(ParentMessageNotification notification, CancellationToken cancellationToken = default)
    {
        try
        {
            var daycareUrl = _configuration["Services:DaycareUrl"] 
                          ?? _configuration["DaycareUrl"] 
                          ?? "https://localhost:7003";

            var endpoint = $"{daycareUrl.TrimEnd('/')}/api/realtime/parent-message";
            var client = _httpClientFactory.CreateClient("DaycareRealtimeClient");
            client.Timeout = TimeSpan.FromSeconds(4);

            _logger.LogInformation("Dispatching real-time notification to {Endpoint} for child {ChildName}", endpoint, notification.ChildName);

            var response = await client.PostAsJsonAsync(endpoint, notification, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Realtime notification returned status code {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            // Fail open: A delivery glitch to the live notification channel must NEVER block core message storage.
            _logger.LogWarning(ex, "Could not dispatch real-time parent message notification to Daycare service. Core message remains securely saved.");
        }
    }

    public async Task NotifyEducatorMessageSentAsync(ParentMessageNotification notification, CancellationToken cancellationToken = default)
    {
        try
        {
            var parentUrl = _configuration["Services:ParentUrl"] 
                          ?? _configuration["ParentUrl"] 
                          ?? "https://localhost:7005";

            var endpoint = $"{parentUrl.TrimEnd('/')}/api/realtime/educator-message";
            var client = _httpClientFactory.CreateClient("ParentRealtimeClient");
            client.Timeout = TimeSpan.FromSeconds(4);

            _logger.LogInformation("Dispatching real-time educator notification to {Endpoint} for child {ChildName}", endpoint, notification.ChildName);

            var response = await client.PostAsJsonAsync(endpoint, notification, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Realtime educator notification returned status code {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            // Fail open: A delivery glitch to the live notification channel must NEVER block core message storage.
            _logger.LogWarning(ex, "Could not dispatch real-time educator message notification to Parent service. Core message remains securely saved.");
        }
    }
}
