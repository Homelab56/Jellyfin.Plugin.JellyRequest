using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.JellyRequest.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyRequest.Services
{
    /// <summary>
    /// Background service for polling request status updates.
    /// </summary>
    public class PollingService : BackgroundService
    {
        private readonly RequestService _requestService;
        private readonly PluginConfiguration _config;
        private readonly ILogger<PollingService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PollingService"/> class.
        /// </summary>
        /// <param name="requestService">The request service.</param>
        /// <param name="config">The plugin configuration.</param>
        /// <param name="logger">The logger.</param>
        public PollingService(RequestService requestService, PluginConfiguration config, ILogger<PollingService> logger)
        {
            _requestService = requestService;
            _config = config;
            _logger = logger;
        }

        /// <inheritdoc />
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("JellyRequest PollingService started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _requestService.UpdateRequestStatusesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during request status polling");
                }

                // Wait for the configured interval
                var delay = TimeSpan.FromMinutes(_config.PollingIntervalMinutes);
                await Task.Delay(delay, stoppingToken);
            }

            _logger.LogInformation("JellyRequest PollingService stopped");
        }
    }
}
