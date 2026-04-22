using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JellyRequest.Services
{
    /// <summary>
    /// Service for interacting with Sonarr API.
    /// </summary>
    public class SonarrService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SonarrService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SonarrService"/> class.
        /// </summary>
        /// <param name="httpClientFactory">The HTTP client factory.</param>
        /// <param name="logger">The logger.</param>
        public SonarrService(IHttpClientFactory httpClientFactory, ILogger<SonarrService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <summary>
        /// Test connection to Sonarr.
        /// </summary>
        /// <param name="url">The Sonarr URL.</param>
        /// <param name="apiKey">The Sonarr API key.</param>
        /// <returns>True if connection is successful.</returns>
        public async Task<bool> TestConnectionAsync(string url, string apiKey)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await GetAsync<List<SonarrSeries>>(client, $"{url}/api/v3/series", apiKey);
                return response != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing Sonarr connection");
                return false;
            }
        }

        /// <summary>
        /// Get all series from Sonarr.
        /// </summary>
        /// <param name="url">The Sonarr URL.</param>
        /// <param name="apiKey">The Sonarr API key.</param>
        /// <returns>List of series.</returns>
        public async Task<List<SonarrSeries>> GetSeriesAsync(string url, string apiKey)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                return await GetAsync<List<SonarrSeries>>(client, $"{url}/api/v3/series", apiKey) ?? new List<SonarrSeries>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting series from Sonarr");
                return new List<SonarrSeries>();
            }
        }

        /// <summary>
        /// Add a series to Sonarr.
        /// </summary>
        /// <param name="url">The Sonarr URL.</param>
        /// <param name="apiKey">The Sonarr API key.</param>
        /// <param name="tmdbId">The TMDB ID.</param>
        /// <param name="qualityProfileId">The quality profile ID.</param>
        /// <param name="rootFolderPath">The root folder path.</param>
        /// <param name="title">The series title.</param>
        /// <param name="year">The release year.</param>
        /// <returns>The added series, or null if failed.</returns>
        public async Task<SonarrSeries?> AddSeriesAsync(string url, string apiKey, int tmdbId, int qualityProfileId, string rootFolderPath, string title, int year)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                
                // First, lookup the series to get the correct details
                var lookupResponse = await GetAsync<List<SonarrSeries>>(
                    client, 
                    $"{url}/api/v3/series/lookup", 
                    apiKey, 
                    new Dictionary<string, string> { ["term"] = $"tmdb:{tmdbId}" });

                if (lookupResponse == null || !lookupResponse.Any())
                {
                    _logger.LogWarning("Series not found in Sonarr lookup: TMDB {TmdbId}", tmdbId);
                    return null;
                }

                var series = lookupResponse.First();
                series.QualityProfileId = qualityProfileId;
                series.RootFolderPath = rootFolderPath;
                series.Monitored = true;
                series.Added = DateTime.UtcNow;
                series.Seasons = series.Seasons?.Select(s => new SonarrSeason
                {
                    SeasonNumber = s.SeasonNumber,
                    Monitored = true
                }).ToList() ?? new List<SonarrSeason>();

                var json = JsonSerializer.Serialize(series, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{url}/api/v3/series?apikey={apiKey}", content);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<SonarrSeries>(responseJson, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding series to Sonarr: TMDB {TmdbId}", tmdbId);
                return null;
            }
        }

        /// <summary>
        /// Delete a series from Sonarr.
        /// </summary>
        /// <param name="url">The Sonarr URL.</param>
        /// <param name="apiKey">The Sonarr API key.</param>
        /// <param name="seriesId">The Sonarr series ID.</param>
        /// <returns>True if successful.</returns>
        public async Task<bool> DeleteSeriesAsync(string url, string apiKey, int seriesId)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.DeleteAsync($"{url}/api/v3/series/{seriesId}?apikey={apiKey}&deleteFiles=true");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting series from Sonarr: {SeriesId}", seriesId);
                return false;
            }
        }

        /// <summary>
        /// Get the queue from Sonarr.
        /// </summary>
        /// <param name="url">The Sonarr URL.</param>
        /// <param name="apiKey">The Sonarr API key.</param>
        /// <returns>The queue items.</returns>
        public async Task<List<SonarrQueueItem>> GetQueueAsync(string url, string apiKey)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                return await GetAsync<List<SonarrQueueItem>>(client, $"{url}/api/v3/queue", apiKey) ?? new List<SonarrQueueItem>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting queue from Sonarr");
                return new List<SonarrQueueItem>();
            }
        }

        /// <summary>
        /// Get quality profiles from Sonarr.
        /// </summary>
        /// <param name="url">The Sonarr URL.</param>
        /// <param name="apiKey">The Sonarr API key.</param>
        /// <returns>List of quality profiles.</returns>
        public async Task<List<SonarrQualityProfile>> GetQualityProfilesAsync(string url, string apiKey)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                return await GetAsync<List<SonarrQualityProfile>>(client, $"{url}/api/v3/qualityprofile", apiKey) ?? new List<SonarrQualityProfile>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quality profiles from Sonarr");
                return new List<SonarrQualityProfile>();
            }
        }

        /// <summary>
        /// Get root folders from Sonarr.
        /// </summary>
        /// <param name="url">The Sonarr URL.</param>
        /// <param name="apiKey">The Sonarr API key.</param>
        /// <returns>List of root folders.</returns>
        public async Task<List<SonarrRootFolder>> GetRootFoldersAsync(string url, string apiKey)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                return await GetAsync<List<SonarrRootFolder>>(client, $"{url}/api/v3/rootfolder", apiKey) ?? new List<SonarrRootFolder>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting root folders from Sonarr");
                return new List<SonarrRootFolder>();
            }
        }

        private async Task<T?> GetAsync<T>(HttpClient client, string url, string apiKey, Dictionary<string, string>? parameters = null)
        {
            var fullUrl = $"{url}?apikey={apiKey}";
            if (parameters != null)
            {
                var queryString = string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
                fullUrl += $"&{queryString}";
            }

            var response = await client.GetAsync(fullUrl);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        }
    }

    // Sonarr Models
    public class SonarrSeries
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Year { get; set; }
        public int TmdbId { get; set; }
        public int QualityProfileId { get; set; }
        public string RootFolderPath { get; set; } = string.Empty;
        public bool Monitored { get; set; }
        public DateTime Added { get; set; }
        public string? Status { get; set; }
        public bool HasFile { get; set; }
        public string? PosterPath { get; set; }
        public string? BackdropPath { get; set; }
        public List<SonarrSeason>? Seasons { get; set; }
    }

    public class SonarrSeason
    {
        public int SeasonNumber { get; set; }
        public bool Monitored { get; set; }
    }

    public class SonarrQueueItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int TmdbId { get; set; }
        public string Status { get; set; } = string.Empty;
        public double? Sizeleft { get; set; }
        public double? Size { get; set; }
        public string? DownloadId { get; set; }
        public DateTime? Timeleft { get; set; }
    }

    public class SonarrQualityProfile
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class SonarrRootFolder
    {
        public int Id { get; set; }
        public string Path { get; set; } = string.Empty;
        public int? FreeSpace { get; set; }
    }
}
