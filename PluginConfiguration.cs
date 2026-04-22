using System;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.JellyRequest.Configuration
{
    /// <summary>
    /// Configuration class for JellyRequest plugin.
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// Gets or sets the TMDB API key.
        /// </summary>
        public string TmdbApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Radarr URL.
        /// </summary>
        public string RadarrUrl { get; set; } = "http://localhost:7878";

        /// <summary>
        /// Gets or sets the Radarr API key.
        /// </summary>
        public string RadarrApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Sonarr URL.
        /// </summary>
        public string SonarrUrl { get; set; } = "http://localhost:8989";

        /// <summary>
        /// Gets or sets the Sonarr API key.
        /// </summary>
        public string SonarrApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the default Radarr quality profile ID.
        /// </summary>
        public int RadarrQualityProfileId { get; set; } = 1;

        /// <summary>
        /// Gets or sets the default Sonarr quality profile ID.
        /// </summary>
        public int SonarrQualityProfileId { get; set; } = 1;

        /// <summary>
        /// Gets or sets the default Radarr root folder ID.
        /// </summary>
        public int RadarrRootFolderId { get; set; } = 1;

        /// <summary>
        /// Gets or sets the default Sonarr root folder ID.
        /// </summary>
        public int SonarrRootFolderId { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value indicating whether regular users can make requests.
        /// </summary>
        public bool AllowRegularUserRequests { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to enable notifications.
        /// </summary>
        public bool EnableNotifications { get; set; } = true;

        /// <summary>
        /// Gets or sets the polling interval in minutes for checking request status.
        /// </summary>
        public int PollingIntervalMinutes { get; set; } = 5;

        /// <summary>
        /// Gets or sets the maximum number of requests per user.
        /// </summary>
        public int MaxRequestsPerUser { get; set; } = 10;

        /// <summary>
        /// Gets or sets the language code for TMDB API calls.
        /// </summary>
        public string TmdbLanguage { get; set; } = "en-US";

        /// <summary>
        /// Gets or sets a value indicating whether to include adult content in search results.
        /// </summary>
        public bool IncludeAdultContent { get; set; } = false;
    }
}
